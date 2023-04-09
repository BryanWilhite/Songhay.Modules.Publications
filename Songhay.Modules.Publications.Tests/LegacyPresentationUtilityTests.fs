namespace Songhay.Modules.Publications.Tests

open System.IO
open System.Reflection
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

open Songhay.Modules.Bolero.LegacyPresentationUtility

type LegacyPresentationUtilityTests(outputHelper: ITestOutputHelper) =

    let projectDirectoryInfo =
        Assembly.GetExecutingAssembly()
        |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
        |> Result.valueOr raiseProgramFileError
        |> DirectoryInfo

    let audioJsonDocumentPath =
        "./json/progressive-audio-default.json"
        |> tryGetCombinedPath projectDirectoryInfo.FullName
        |> Result.valueOr raiseProgramFileError

    let presentationElementResult =
        File.ReadAllText(audioJsonDocumentPath)
        |> tryGetPresentationElementResult

    [<Theory>]
    [<InlineData("2005-12-10-22-19-14-IDAMAQDBIDANAQDB-1")>]
    let ``Presentation.id test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationIdResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Identifier.fromString |> Id)
        actual |> should be (ofCase <@ Result<Id, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).Value.StringValue |> should equal expected

    [<Theory>]
    [<InlineData("Songhay Audio Presentation")>]
    let ``Presentation.title test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationTitleResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toResultFromStringElement (fun el -> el.GetString() |> Title)
        actual |> should be (ofCase <@ Result<Title, JsonException>.Ok @>)

        match (actual |> Result.valueOr raise) with | Title t -> t |> should equal expected

    [<Theory>]
    [<InlineData("This InfoPath Form data is packaged with the audio presentation")>]
    let ``Presentation.parts PresentationDescription test`` (expected: string) =
        let result = presentationElementResult |> tryGetPresentationDescriptionResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual =
            result
            |> toResultFromStringElement (fun el -> el.GetString() |> PresentationDescription)
        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).StringValue.Contains(expected) |> should be True

    [<Fact>]
    let ``Presentation.parts Credits test`` () =
        let result = presentationElementResult |> tryGetPresentationCreditsResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationCreditsResult
        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

        (actual |> Result.valueOr raise) |> should not' (be Empty)

    [<Theory>]
    [<InlineData("--rx-player-playlist-background-color", "0xEAEAEA")>]
    let ``Presentation.cssVariables test``(expectedVarName: string) (expectedValue: string) =
        let result = presentationElementResult |> tryGetLayoutMetadataResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationCssVariablesResult
        actual |> should be (ofCase <@ Result<CssVariableAndValues, JsonException>.Ok @>)
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
    [<InlineData("©2006 Songhay System")>]
    let ``Presentation.parts Copyrights test`` (expected: string) =
        let nameElementResult = presentationElementResult |> tryGetCopyrightNameResult
        nameElementResult |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let yearElementResult = presentationElementResult |> tryGetCopyrightYearResult
        yearElementResult |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = (nameElementResult, yearElementResult) ||> toPresentationCopyrights

        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

        (actual |> Result.valueOr raise).StringValues[0] |> should equal expected

    [<Fact>]
    let ``Presentation.parts Playlist test`` () =
        let result = presentationElementResult |> tryGetPlaylistRootResult
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let actual = result |> toPresentationPlaylistResult

        actual |> should be (ofCase <@ Result<PresentationPart, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetPresentation test`` () =
        let json = File.ReadAllText(audioJsonDocumentPath)
        let actual = json |> tryGetPresentation
        actual |> should be (ofCase <@ Result<Presentation, JsonException>.Ok @>)
