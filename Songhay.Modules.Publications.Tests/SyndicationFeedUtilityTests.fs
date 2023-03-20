namespace Songhay.Modules.Publications.Tests

open System

module SyndicationFeedUtilityTests =

    open System.IO
    open System.Reflection
    open System.Text.Json

    open Xunit

    open FsUnit.Xunit
    open FsUnit.CustomMatchers
    open FsToolkit.ErrorHandling

    open Songhay.Modules.Models
    open Songhay.Modules.ProgramFileUtility
    open Songhay.Modules.JsonDocumentUtility
    open Songhay.Modules.Publications.SyndicationFeedUtility

    let projectDirectoryInfo =
        Assembly.GetExecutingAssembly()
        |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
        |> Result.valueOr raiseProgramFileError
        |> DirectoryInfo

    let jsonPath =
        $"./json/syndication-feed-test-data.json"
        |> tryGetCombinedPath projectDirectoryInfo.FullName
        |> Result.valueOr raiseProgramFileError

    let jsonRootElementResult = tryGetRootElement <| File.ReadAllText jsonPath

    let rssRootElement = jsonRootElementResult |> Result.bind (tryGetProperty RssFeedPropertyName) |> Result.valueOr raise
    let atomRootElement = jsonRootElementResult |> Result.bind (tryGetProperty AtomFeedPropertyName) |> Result.valueOr raise

    [<Fact>]
    let ``isAtomFeed test for true``() =
        let actual = atomRootElement |> isAtomFeed
        actual |> should equal true

    [<Fact>]
    let ``isAtomFeed test for false``() =
        let actual = rssRootElement |> isAtomFeed
        actual |> should equal false

    [<Fact>]
    let ``isRssFeed test for true``() =
        let actual = rssRootElement |> isRssFeed
        actual |> should equal true

    [<Fact>]
    let ``isRssFeed test for false``() =
        let actual = atomRootElement |> isRssFeed
        actual |> should equal false

    [<Fact>]
    let ``tryGetFeedElement Atom test``() =
        let json = @$"{{ ""{AtomFeedPropertyName}"": {atomRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result |> should be (ofCase <@ Result<bool * JsonElement, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetFeedElement Atom failure test``() =
        let json = @$"{{ ""{AtomFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result |> should be (ofCase <@ Result<bool * JsonElement, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetFeedElement RSS test``() =
        let json = @$"{{ ""{RssFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result |> should be (ofCase <@ Result<bool * JsonElement, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetFeedElement RSS failure test``() =
        let json = @$"{{ ""{RssFeedPropertyName}"": {atomRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result |> should be (ofCase <@ Result<bool * JsonElement, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetFeedModificationDate Atom test``() =
        let result = atomRootElement |> tryGetFeedModificationDate (isRssFeed atomRootElement)
        result |> should be (ofCase <@ Result<DateTime, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetFeedModificationDate RSS test``() =
        let result = rssRootElement |> tryGetFeedModificationDate (isRssFeed rssRootElement)
        result |> should be (ofCase <@ Result<DateTime, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetSyndicationFeedItem test``() =
        let result = (Ok "title", Ok "urn:link") |> tryGetSyndicationFeedItem
        result |> should be (ofCase <@ Result<SyndicationFeedItem, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetSyndicationFeedItem failure test``() =
        let result = (Ok "title", Error <| JsonException "JSON problem") |> tryGetSyndicationFeedItem
        result |> should be (ofCase <@ Result<SyndicationFeedItem, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetAtomEntries test``() =
        let result = atomRootElement |> tryGetAtomEntries
        result |> should be (ofCase <@ Result<JsonElement list, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetAtomEntries failure test``() =
        let result = rssRootElement |> tryGetAtomEntries
        result |> should be (ofCase <@ Result<JsonElement list, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetAtomChannelTitle test``() =
        let result = atomRootElement |> tryGetAtomChannelTitle
        result |> should be (ofCase <@ Result<string, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetAtomChannelTitle failure test``() =
        let result = rssRootElement |> tryGetAtomChannelTitle
        result |> should be (ofCase <@ Result<string, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetRssChannelTitle test``() =
        let result = rssRootElement |> tryGetRssChannelTitle
        result |> should be (ofCase <@ Result<string, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetRssChannelTitle failure test``() =
        let result = atomRootElement |> tryGetRssChannelTitle
        result |> should be (ofCase <@ Result<string, JsonException>.Error @>)

    [<Fact>]
    let ``tryGetRssChannelItems test``() =
        let result = rssRootElement |> tryGetRssChannelItems
        result |> should be (ofCase <@ Result<JsonElement list, JsonException>.Ok @>)

    [<Fact>]
    let ``tryGetAtomSyndicationFeedItem test``() =
        let elements =
            atomRootElement
            |> tryGetRssChannelItems
            |> Result.valueOr raise
        elements |> List.iter
            (
                 fun el ->
                    let result = el |> tryGetAtomSyndicationFeedItem
                    result |> should be (ofCase <@ Result<SyndicationFeedItem, JsonException>.Ok @>)
            )

    [<Fact>]
    let ``tryGetRssSyndicationFeedItem test``() =
        let elements =
            rssRootElement
            |> tryGetRssChannelItems
            |> Result.valueOr raise
        elements |> List.iter
            (
                 fun el ->
                    let result = el |> tryGetRssSyndicationFeedItem
                    result |> should be (ofCase <@ Result<SyndicationFeedItem, JsonException>.Ok @>)
            )

    [<Fact>]
    let ``tryGetSyndicationFeedsElement test``() =
        let json = @$"{{ ""{RssFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let json = $@"{{ ""{SyndicationFeedPropertyName}"": {{ ""root"": {json} }} }}"

        let result =
            json
            |> tryGetRootElement
            |> Result.bind tryGetSyndicationFeedsElement
        result |> should be (ofCase <@ Result<JsonElement, JsonException>.Ok @>)

        let result =
            result
            |> Result.bind (tryGetProperty "root")
            |> Result.bind tryGetFeedElement
        result |> should be (ofCase <@ Result<bool * JsonElement, JsonException>.Ok @>)
