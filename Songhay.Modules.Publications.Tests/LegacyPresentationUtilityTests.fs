namespace Songhay.Modules.Publications.Tests

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

open FsToolkit.ErrorHandling
open FsUnit.CustomMatchers
open FsUnit.Xunit
open Xunit
open Xunit.Abstractions

open Songhay.Modules.Models
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.Publications.Models
open Songhay.Modules.ProgramFileUtility

open Songhay.Modules.Publications.LegacyPresentationUtility

open Songhay.Modules.Publications.Tests.PublicationsTestUtility

type LegacyPresentationUtilityTests(outputHelper: ITestOutputHelper) =

    let audioJsonDocumentPath =
        "./json/progressive-audio-default.json"
        |> tryGetCombinedPath projectDirectoryInfo.FullName
        |> Result.valueOr raiseProgramFileError

    let presentationElementResult =
        File.ReadAllText(audioJsonDocumentPath)
        |> tryGetPresentationElementResult

    [<Theory>]
    [<InlineData("2005-12-10-22-19-14-IDAMAQDBIDANAQDB-1")>]
    member this.``Presentation.id test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationIdResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Identifier.fromString |> Id)
        actual |> should be (ofCase <@ Result<Id, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).Value.StringValue |> should equal expected

    [<Theory>]
    [<InlineData("Songhay Audio Presentation")>]
    member this.``Presentation.title test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationTitleResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Title)
        actual |> should be (ofCase <@ Result<Title, JsonException>.Ok @>)

        match (actual |> Result.valueOr raise) with | Title t -> t |> should equal expected

    [<Theory>]
    [<InlineData("This InfoPath Form data is packaged with the audio presentation")>]
    member this.``Presentation.parts PresentationDescription test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationDescriptionResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual =
            result
            |> toResultFromStringElement (fun el -> el.GetString() |> PresentationDescription)
        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).StringValue.Contains(expected) |> should be True

    [<Theory>]
    [<InlineData("--rx-player-playlist-background-color", "#eaeaea")>]
    member this.``Presentation.cssVariables test``(expectedVarName: string) (expectedValue: string) =
        let result = presentationElementResult |> tryGetLayoutMetadataResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationCssVariablesResult
        actual |> should be (ofCase <@ Result<CssCustomPropertyAndValue list, JsonException>.Ok @>)
        (actual |> Result.valueOr raise)
        |> List.find
            (
                fun i ->
                    let cssVar, cssVal = i.Pair
                    cssVar.Value = expectedVarName
                    && cssVal.Value = expectedValue
            )
        |> (fun i -> i.toCssDeclaration |> outputHelper.WriteLine)

    [<Fact>]
    member this.``Presentation.parts Playlist test`` () =
        let result = presentationElementResult |> tryGetPlaylistRootResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationPlaylistResult

        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

    [<Theory>]
    [<InlineData("default")>]
    member this.``tryGetPresentation test`` (presentationKey: string) =

        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())

        let inputPath =
            "json/presentation-credits-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        let creditsSet =
            JsonSerializer
                .Deserialize<Dictionary<string,Result<RoleCredit list,ProgramFileError>>>(File.ReadAllText(inputPath), options)

        let json = File.ReadAllText(audioJsonDocumentPath)
        let actual = json |> tryGetPresentation (creditsSet[presentationKey] |> Result.mapError(fun ex -> JsonException $"{ex}"))
        actual |> should be (ofCase <@ Result<Presentation, JsonException>.Ok @>)

        let outputPath =
            $"json/progressive-audio-{presentationKey}-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let json = JsonSerializer.Serialize(actual, options)
        File.WriteAllText(outputPath, json)

        let outputPath =
            $"json/progressive-audio-default-{presentationKey}.scss" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let scssArray =
            actual
            |> Result.valueOr raise
            |> fun presentation ->
                presentation.cssVariables
                |> List.map (fun v -> v.toCssDeclaration)
                |> Array.ofList
        File.WriteAllText(outputPath, String.Join(Environment.NewLine, scssArray))

    [<Theory>]
    //[<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``write Presentation JSON test``(containerName: string) =

        let options = JsonSerializerOptions()
        options.WriteIndented <- true
        options.Converters.Add(JsonFSharpConverter())

        let inputPath =
            $"json/{containerName}-presentation-credits-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        let creditsSet =
            JsonSerializer
                .Deserialize<Dictionary<string,Result<RoleCredit list,ProgramFileError>>>(File.ReadAllText(inputPath), options)

        containerName
        |> getContainerDirectories
        |> List.ofSeq
        |> List.iter
            (
                fun directory ->
                    let presentationKey = directory |> directoryName
                    outputHelper.WriteLine $"processing `{presentationKey}`..."
                    let inputPath =
                        tryGetCombinedPath directory $"{presentationKey}.json"
                        |> Result.valueOr raiseProgramFileError
                    let presentationJson = File.ReadAllText(inputPath)
                    let presentationResult = presentationJson |> tryGetPresentation (creditsSet[presentationKey] |> Result.mapError(fun ex -> JsonException $"{ex}"))
                    presentationResult |> should be (ofCase <@ Result<Presentation, JsonException>.Ok @>)

                    let presentation = presentationResult |> Result.valueOr raise
                    let json = JsonSerializer.Serialize(presentation, options)
                    let outputPath =
                        tryGetCombinedPath directory $"{presentationKey}_presentation.json"
                        |> Result.valueOr raiseProgramFileError

                    File.WriteAllText(outputPath, json)
            )
