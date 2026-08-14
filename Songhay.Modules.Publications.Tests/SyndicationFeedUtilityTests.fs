namespace Songhay.Modules.Publications.Tests

open System.IO
open System.Text.Json
open Xunit

open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result

open Songhay.Modules.ProgramFileUtility
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.Publications.SyndicationFeedUtility

open Songhay.Modules.Publications.Tests.PublicationsTestUtility

module SyndicationFeedUtilityTests =

    let getSyndicationElements() =
        let jsonPath =
            $"./json/syndication-feed-test-data.json"
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let jsonRootElementResult = tryGetRootElement <| File.ReadAllText jsonPath

        let rssRootElement = jsonRootElementResult |> Result.bind (tryGetProperty RssFeedPropertyName) |> Result.valueOr raise
        let atomRootElement = jsonRootElementResult |> Result.bind (tryGetProperty AtomFeedPropertyName) |> Result.valueOr raise

        (rssRootElement, atomRootElement)

    [<Fact>]
    let ``isAtomFeed test for true``() =
        let _, atomRootElement = getSyndicationElements()
        let actual = atomRootElement |> isAtomFeed
        actual |> Assert.True

    [<Fact>]
    let ``isAtomFeed test for false``() =
        let rssRootElement, _ = getSyndicationElements()
        let actual = rssRootElement |> isAtomFeed
        actual |> Assert.False

    [<Fact>]
    let ``isRssFeed test for true``() =
        let rssRootElement, _ = getSyndicationElements()
        let actual = rssRootElement |> isRssFeed
        actual |> Assert.True

    [<Fact>]
    let ``isRssFeed test for false``() =
        let _, atomRootElement = getSyndicationElements()
        let actual = atomRootElement |> isRssFeed
        actual |> Assert.False

    [<Fact>]
    let ``tryGetFeedElement Atom test``() =
        let _, atomRootElement = getSyndicationElements()
        let json = @$"{{ ""{AtomFeedPropertyName}"": {atomRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetFeedElement Atom failure test``() =
        let rssRootElement, _ = getSyndicationElements()
        let json = @$"{{ ""{AtomFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetFeedElement RSS test``() =
        let rssRootElement, _ = getSyndicationElements()
        let json = @$"{{ ""{RssFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetFeedElement RSS failure test``() =
        let _, atomRootElement = getSyndicationElements()
        let json = @$"{{ ""{RssFeedPropertyName}"": {atomRootElement.GetRawText()} }}"
        let element = json |> tryGetRootElement |> Result.valueOr raise
        let result = element |> tryGetFeedElement
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetFeedModificationDate Atom test``() =
        let _, atomRootElement = getSyndicationElements()
        let result = atomRootElement |> tryGetFeedModificationDate (isRssFeed atomRootElement)
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetFeedModificationDate RSS test``() =
        let rssRootElement, _ = getSyndicationElements()
        let result = rssRootElement |> tryGetFeedModificationDate (isRssFeed rssRootElement)
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetSyndicationFeedItem test``() =
        let result = (Ok "title", Ok "urn:link") |> toSyndicationFeedItem
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetSyndicationFeedItem failure test``() =
        let result = (Ok "title", Error <| JsonException "JSON problem") |> toSyndicationFeedItem
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetAtomEntries test``() =
        let _, atomRootElement = getSyndicationElements()
        let result = atomRootElement |> tryGetAtomEntries
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetAtomEntries failure test``() =
        let rssRootElement, _ = getSyndicationElements()
        let result = rssRootElement |> tryGetAtomEntries
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetAtomChannelTitle test``() =
        let _, atomRootElement = getSyndicationElements()
        let result = atomRootElement |> tryGetAtomChannelTitle
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetAtomChannelTitle failure test``() =
        let rssRootElement, _ = getSyndicationElements()
        let result = rssRootElement |> tryGetAtomChannelTitle
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetRssChannelTitle test``() =
        let rssRootElement, _ = getSyndicationElements()
        let result = rssRootElement |> tryGetRssChannelTitle
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetRssChannelTitle failure test``() =
        let _, atomRootElement = getSyndicationElements()
        let result = atomRootElement |> tryGetRssChannelTitle
        result.IsError |> Assert.True

    [<Fact>]
    let ``tryGetRssChannelItems test``() =
        let rssRootElement, _ = getSyndicationElements()
        let result = rssRootElement |> tryGetRssChannelItems
        result.IsOk |> Assert.True

    [<Fact>]
    let ``tryGetAtomSyndicationFeedItem test``() =
        let _, atomRootElement = getSyndicationElements()
        let elements =
            atomRootElement
            |> tryGetAtomEntries
            |> Result.valueOr raise
        elements |> List.iter
            (
                 fun el ->
                    let result = el |> tryGetAtomSyndicationFeedItem
                    result.IsOk |> Assert.True
            )

    [<Fact>]
    let ``tryGetRssSyndicationFeedItem test``() =
        let rssRootElement, _ = getSyndicationElements()
        result {

            let! elements = rssRootElement |> tryGetRssChannelItems

            elements |> List.iter
                (
                     fun el ->
                        let result = el |> tryGetRssSyndicationFeedItem
                        result.IsOk |> Assert.True
                )

            return ()
        }
        |> ignore

    [<Fact>]
    let ``tryGetSyndicationFeedsElement test``() =
        let rssRootElement, _ = getSyndicationElements()
        let json = @$"{{ ""{RssFeedPropertyName}"": {rssRootElement.GetRawText()} }}"
        let json = $@"{{ ""{SyndicationFeedPropertyName}"": {{ ""root"": {json} }} }}"

        let result =
            json
            |> tryGetRootElement
            >>= tryGetSyndicationFeedsElement
        result.IsOk |> Assert.True

        let result =
            result
            >>= (tryGetProperty "root")
            >>= tryGetFeedElement
        result.IsOk |> Assert.True
