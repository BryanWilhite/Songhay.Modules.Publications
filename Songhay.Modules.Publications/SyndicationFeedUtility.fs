namespace Songhay.Modules.Publications

module SyndicationFeedUtility =

    open System
    open System.Text.Json

    open FsToolkit.ErrorHandling

    open Microsoft.FSharp.Core

    open Songhay.Modules.Models

    open Songhay.Modules.JsonDocumentUtility
    open Songhay.Modules.ProgramTypeUtility

    ///<summary>
    /// Defines the expected root XML element of an Atom feed.
    /// </summary>
    /// <remarks>
    /// XML namespace: http://www.w3.org/2005/Atom
    /// </remarks>
    [<Literal>]
    let AtomFeedPropertyName = "feed"

    ///<summary>
    /// Defines the expected root XML element of an RSS feed.
    /// </summary>
    /// <remarks>
    /// specification: https://www.rssboard.org/rss-specification
    /// </remarks>
    [<Literal>]
    let RssFeedPropertyName = "rss"

    ///<summary>
    /// Defines the expected root JSON property name
    /// of the conventional <c>app.json</c> file of this Studio.
    /// </summary>
    /// <remarks>
    /// This convention exists because XML Atom/RSS feeds are converted to JSON
    /// and aggregated in <c>app.json</c>.
    /// </remarks>
    [<Literal>]
    let SyndicationFeedPropertyName = "feeds"

    ///<summary>
    /// Returns <c>true</c> when the specified <see cref="JsonDocumentOrElement" />
    /// appears to be an RSS feed, converted from XML.
    /// </summary>
    let isRssFeed (elementName: string) (documentOrElement: JsonDocumentOrElement) =
        let elementNameNormalized = elementName.ToLowerInvariant()

        documentOrElement
        |> tryGetProperty SyndicationFeedPropertyName
        |> Result.bind (tryGetProperty elementNameNormalized)
        |> Result.bind (tryGetProperty RssFeedPropertyName)
        |> Result.map (fun _ -> true)
        |> Result.valueOr (fun _ -> false)

    ///<summary>
    /// Tries to get the root element of the converted RSS or Atom feed
    /// from the specified <see cref="JsonDocumentOrElement" />.
    /// </summary>
    let tryGetFeedElement (elementName: string) (documentOrElement: JsonDocumentOrElement) =
        let elementNameNormalized = elementName.ToLowerInvariant()

        let getElement (feedPropertyName: string) =
            documentOrElement |> tryGetProperty SyndicationFeedPropertyName
            |> Result.bind (tryGetProperty elementNameNormalized)
            |> Result.bind (tryGetProperty feedPropertyName)

        match documentOrElement |> isRssFeed elementNameNormalized with
        | true -> (getElement RssFeedPropertyName) |> Result.map (fun rssElement -> true, rssElement)
        | _ -> (getElement AtomFeedPropertyName) |> Result.map (fun atomElement -> false, atomElement)

    ///<summary>
    /// Tries to get the RSS or Atom feed modification date
    /// from the specified <see cref="JsonDocumentOrElement" />.
    /// </summary>
    let tryGetFeedModificationDate (isRssFeed: bool) (documentOrElement: JsonDocumentOrElement) =
        match isRssFeed with
        | false ->
            documentOrElement
            |> tryGetProperty "updated"
            |> Result.map toJsonElement
            |> toResultFromStringElement (fun updatedElement -> updatedElement.GetDateTime())
        | true ->
            documentOrElement
            |> tryGetProperty "channel"
            |> Result.either
                (
                    fun channelElement ->
                        channelElement
                        |> tryGetProperty "pubDate"
                        |> Result.map toJsonElement
                        |> Result.either
                            (
                                fun pubDateElement ->
                                    let dateTimeString = pubDateElement.GetString().Trim()

                                    match tryParseRfc822DateTime dateTimeString with
                                    | Error _ -> resultError "pubDate"
                                    | Ok rfc822DateTime -> Ok rfc822DateTime
                            )
                            (
                                fun _ ->
                                    channelElement
                                    |> tryGetProperty "dc:date"
                                    |> Result.map toJsonElement
                                    |> Result.either
                                        (
                                            fun pubDateElement ->
                                                let dateTimeString = pubDateElement.GetString().Trim()

                                                match DateTime.TryParse dateTimeString with
                                                | false, _ -> resultError "dc:date"
                                                | true, dateTime -> Ok dateTime
                                        )
                                        Error
                            )
                )
                Error

    ///<summary>
    /// Tries to get a <see cref="SyndicationFeedItem" />
    /// from the specified tuple.
    /// </summary>
    let tryGetSyndicationFeedItem (titleResult: Result<string,JsonException>, linkResult: Result<string,JsonException>) =
        [
            titleResult
            linkResult
        ]
        |> List.sequenceResultM
        |> Result.either
            (
                fun _ ->
                    Ok {
                        title = titleResult |> Result.valueOr raise
                        link = linkResult |> Result.valueOr raise
                        extract = None
                        publicationDate = None
                    }
            )
            Error

    ///<summary>
    /// Tries to call <see cref="tryGetSyndicationFeedItem" />
    /// after reading the specified <see cref="JsonElement" />
    /// as an Atom <c>entry</c>.
    /// </summary>
    let tryGetAtomSyndicationFeedItem (el: JsonElement) =

        let titleResult =
            JElement el
            |> tryGetProperty "title"
            |> Result.bind (tryGetProperty "#text")
            |> Result.map toJsonElement
            |> Result.map (fun textElement -> textElement.GetString())

        let linkResult =
            JElement el
            |> tryGetProperty "link"
            |> Result.bind (tryGetProperty "@href")
            |> Result.map toJsonElement
            |> Result.map (fun hrefElement -> hrefElement.GetString())

        (titleResult, linkResult) |> tryGetSyndicationFeedItem

    ///<summary>
    /// Tries to return a list of <see cref="JsonElement" />
    /// from the specified <see cref="JsonElement" />
    /// that should contain an Atom <c>entry</c> array.
    /// </summary>
    let tryGetAtomEntries (element: JsonElement) =
        JElement element
        |> tryGetProperty "entry"
        |> Result.map toJsonElement
        |> Result.map (fun el -> el.EnumerateArray() |> List.ofSeq)

    ///<summary>
    /// Tries to get the Atom feed title
    /// from the specified <see cref="JsonElement" />.
    /// </summary>
    let tryGetAtomChannelTitle (element: JsonElement) : Result<string, JsonException> =
        match JElement element |> tryGetProperty "title" with
        | Error err -> Error err
        | Ok documentOrElement ->
            let titleElement = documentOrElement |> toJsonElement
            match titleElement.ValueKind with
            | JsonValueKind.String -> Ok(titleElement.GetString())
            | JsonValueKind.Object ->
                JElement titleElement
                |> tryGetProperty "#text"
                |> Result.map toJsonElement
                |> toResultFromStringElement (fun textElement -> textElement.GetString())
            | _ -> resultError (nameof titleElement)

    ///<summary>
    /// Tries to call <see cref="tryGetSyndicationFeedItem" />
    /// after reading the specified <see cref="JsonElement" />
    /// as an element in the RSS <c>item</c> array.
    /// </summary>
    let tryGetRssSyndicationFeedItem (el: JsonElement) =

        let titleResult =
            JElement el
            |> tryGetProperty "title"
            |> Result.map toJsonElement
            |> toResultFromStringElement (fun titleElement -> titleElement.GetString())

        let linkResult =
            JElement el
            |> tryGetProperty "link"
            |> Result.map toJsonElement
            |> toResultFromStringElement (fun linkElement -> linkElement.GetString())

        (titleResult, linkResult) |> tryGetSyndicationFeedItem

    ///<summary>
    /// Tries to return a list of <see cref="JsonElement" />
    /// from the specified <see cref="JsonElement" />
    /// that should contain an RSS <c>item</c> array.
    /// </summary>
    let tryGetRssChannelItems (element: JsonElement) =
        JElement element
        |> tryGetProperty "channel"
        |> Result.bind (tryGetProperty "item")
        |> Result.map toJsonElement
        |> toResultFromStringElement (fun el -> el.EnumerateArray() |> List.ofSeq)

    ///<summary>
    /// Tries to get the RSS feed title
    /// from the specified <see cref="JsonElement" />.
    /// </summary>
    let tryGetRssChannelTitle (element: JsonElement) =
        JElement element |> tryGetProperty "channel"
        |> Result.bind (tryGetProperty "title")
        |> Result.map toJsonElement
        |> toResultFromStringElement (fun titleElement -> titleElement.GetString())
