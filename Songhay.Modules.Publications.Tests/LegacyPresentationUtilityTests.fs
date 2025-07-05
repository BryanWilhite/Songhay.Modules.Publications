namespace Songhay.Modules.Publications.Tests

open System
open System.Collections.Generic
open System.IO
open System.Text.Json

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

    let getPresentationElementResult path =
        File.ReadAllText(path)
        |> tryGetPresentationElementResult

    [<Theory>]
    [<InlineData(audioContainerName, "default", "2005-12-10-22-19-14-IDAMAQDBIDANAQDB-1")>]
    [<InlineData(videoContainerName, "bowie0", "2008-10-28-22-48-06-IDANAYZBIDAOAYZB-1")>]
    member this.``Presentation.id test`` (containerName: string) (containerKey: string) (expected: string) =
        outputHelper.WriteLine $"checking `{containerKey}` in container {containerName}..."

        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let result = path |> getPresentationElementResult |> tryGetPresentationIdResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Identifier.fromString |> Id)
        actual |> should be (ofCase <@ Result<Id, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).Value.StringValue |> should equal expected

    [<Theory>]
    [<InlineData(audioContainerName, "default", "Songhay Audio Presentation")>]
    [<InlineData(videoContainerName, "bowie0", "the rasx() Bowie Collection (YouTube.com)")>]
    member this.``Presentation.title test`` (containerName: string) (containerKey: string) (expected: string) =
        outputHelper.WriteLine $"checking `{containerKey}` in container {containerName}..."

        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let result = path |> getPresentationElementResult |> tryGetPresentationTitleResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Title)
        actual |> should be (ofCase <@ Result<Title, JsonException>.Ok @>)

        match (actual |> Result.valueOr raise) with | Title t -> t |> should equal expected

    [<Theory>]
    [<InlineData(audioContainerName, "default", "This InfoPath Form data is packaged with the audio presentation")>]
    member this.``Presentation.parts PresentationDescription test`` (containerName: string) (containerKey: string) (expected: string) =
        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let result = path |> getPresentationElementResult |> tryGetPresentationDescriptionResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual =
            result
            |> toResultFromStringElement (fun el -> el.GetString() |> PresentationDescription)
        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).StringValue.Contains(expected) |> should be True

    [<Theory>]
    [<InlineData(audioContainerName, "default", "--rx-player-playlist-background-color", "#eaeaea")>]
    [<InlineData(videoContainerName, "bowie0", "--rx-player-playlist-background-color", "#000")>]
    member this.``Presentation.cssVariables test`` (containerName: string) (containerKey: string) (expectedVarName: string) (expectedValue: string) =
        outputHelper.WriteLine $"checking `{containerKey}` in container {containerName}..."

        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let result = path |> getPresentationElementResult |> tryGetLayoutMetadataResult
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

    [<Theory>]
    [<InlineData(audioContainerName, "default")>]
    [<InlineData(videoContainerName, "bowie0")>]
    member this.``Presentation.parts Playlist test`` (containerName: string) (containerKey: string) =
        outputHelper.WriteLine $"checking `{containerKey}` in container {containerName}..."

        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let result = path |> getPresentationElementResult |> tryGetPlaylistRootResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationPlaylistResult

        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

    [<Theory>]
    [<InlineData(audioContainerName, "default")>]
    [<InlineData(videoContainerName, "bowie0")>]
    member this.``tryGetPresentation test`` (containerName: string) (containerKey: string) =

        let inputPathForCredits =
            $"json/{containerName}-presentation-credits-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        let creditsSet =
            JsonSerializer
                .Deserialize<Dictionary<string,Result<RoleCredit list,ProgramFileError>>>(File.ReadAllText(inputPathForCredits), jsonSerializerOptions())

        let path = getStorageMirrorPath containerName $"{containerKey}/{containerKey}.json"
        let json = File.ReadAllText(path)
        let actual = json |> tryGetPresentation (creditsSet[containerKey] |> Result.mapError(fun ex -> JsonException $"{ex}"))
        actual |> should be (ofCase <@ Result<Presentation, JsonException>.Ok @>)

        let outputPath =
            $"json/{containerName}-{containerKey}-presentation-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine $"writing to `{outputPath}`..."
        let json = JsonSerializer.Serialize(actual, jsonSerializerOptions())
        File.WriteAllText(outputPath, json)

        let scssArray =
            actual
            |> Result.valueOr raise
            |> fun presentation ->
                presentation.cssCustomPropertiesAndValues
                |> List.map (_.toCssDeclaration)
                |> Array.ofList

        outputHelper.WriteLine(String.Join(Environment.NewLine, scssArray))

    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``write Presentation JSON to storage mirror test``(containerName: string) =

        let inputPath =
            $"json/{containerName}-presentation-credits-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        let creditsSet =
            JsonSerializer
                .Deserialize<Dictionary<string,Result<RoleCredit list,ProgramFileError>>>(File.ReadAllText(inputPath), jsonSerializerOptions())

        containerName
        |> getContainerDirectories
        |> List.ofSeq
        |> List.filter (fun path ->
                let dir = path |> directoryName
                [ "css"; "youtube-channels"; "youtube-uploads" ] |> List.contains dir |> not
            )
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
                    let json = JsonSerializer.Serialize(presentation, jsonSerializerOptions())
                    let outputPath =
                        tryGetCombinedPath directory $"{presentationKey}_presentation.json"
                        |> Result.valueOr raiseProgramFileError

                    File.WriteAllText(outputPath, json)
            )
