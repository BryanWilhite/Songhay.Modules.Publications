namespace Songhay.Modules.Bolero

open System
open System.Data
open System.Linq
open System.Collections.Generic
open System.Text.Json
open System.Text.RegularExpressions

open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result

open Songhay.Modules.Models
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.Publications.Models

///<summary>
/// Utility functions for <see cref="Presentation" />
/// to convert legacy JSON data.
/// </summary>
module LegacyPresentationUtility =

    /// <summary>
    /// Converts the specified <see cref="Result{_,_}"/> data
    /// to <see cref="PresentationPart.CopyRights"/>
    /// </summary>
    let toPresentationCopyrights
        (nameElementResult: Result<JsonElement, JsonException>)
        (yearElementResult: Result<JsonElement, JsonException>) =

        result {
            let! name = nameElementResult |> toJsonStringValue
            and! year = yearElementResult |> toJsonIntValueFromStringElement

            return
                [
                    {
                        name = name
                        year = year
                    }
                ]
                |> CopyRights
        }

    /// <summary>
    /// Converts the specified <see cref="Result{_,_}"/> data
    /// to <see cref="PresentationPart.Credits"/>
    /// </summary>
    let toPresentationCreditsResult (elementResult: Result<JsonElement, JsonException>) =

        let rx = Regex("<div>([^<>]+)<strong>([^<>]+)<\/strong><\/div>", RegexOptions.Compiled)
        let matchesResult =
            elementResult
            |> toResultFromStringElement (fun el -> el.GetString())
            |> Result.map (fun html -> html |> rx.Matches)
            >>= (fun matches ->
                if matches.Count > 0 then Ok matches
                else Error <| JsonException "The expected HTML format is not here.")

        let processMatches (creditsMatch: Match) =
            let getRole (group: Group) = Regex.Replace(group.Value, " by[ , ]. . . . . . . ", String.Empty)

            match creditsMatch.Groups |> List.ofSeq with
            | [_; r; n] -> Ok { role = r |> getRole; name = n.Value }
            | _ -> Error <| JsonException ("See inner exception.", DataException $"The expected {nameof(Regex)} group data is not here.")

        matchesResult
        >>= (
            fun matches ->
                matches
                |> Seq.map processMatches
                |> List.ofSeq
                |> List.sequenceResultM
                |> Result.map (fun l -> l  |> Credits)
            )

    /// <summary>
    /// Converts the specified <see cref="Result{_,_}"/> data
    /// to <see cref="PresentationPart.Credits"/>
    /// </summary>
    let toPresentationCssVariablesResult (elementResult: Result<JsonElement, JsonException>) =
        let declarations = List<CssVariableAndValue>()
        let rec processProperty (prefix: string) (p: JsonProperty) =
            match p.Value.ValueKind with
            | JsonValueKind.Object ->
                p.Value.EnumerateObject().ToArray()
                |> Array.iter (fun el -> ($"{prefix}{p.Name}-", el) ||> processProperty)
                ()
            | JsonValueKind.String ->
                match p.Name with
                | "@title" | "@uri" | "@version" -> ()
                | _ ->
                    let cssVal =
                        match p.Name with
                        | "@x" | "@y" | "@marginBottom" | "@marginTop"
                        | "@width" | "@height" -> $"{p.Value.GetString()}px"
                        | "@opacity" -> $"{p.Value.GetString()}%%"
                        | _ -> p.Value.GetString()
                        |> CssValue
                    let cssVar = $"{prefix}{p.Name.TrimStart('@')}" |> CssVariable.fromInput
                    declarations.Add((cssVar, cssVal) |> CssVariableAndValue)
                    ()
            | _ -> ()

        elementResult
        |> toResultFromJsonElement
            (fun kind -> kind = JsonValueKind.Object)
            (fun el -> el.EnumerateObject().ToArray())
        |> Result.map
            (
                fun jsonProperties ->
                    jsonProperties |> Array.iter (fun el -> ("rx-player-", el) ||> processProperty)
                    declarations |> List.ofSeq
            )

    /// <summary>
    /// Converts the specified <see cref="Result{_,_}"/> data
    /// to <see cref="PresentationPart.Playlist"/>
    /// </summary>
    let toPresentationPlaylistResult (elementResult: Result<JsonElement, JsonException>) =

        let toPlaylistItem el =
            result {
                let! title = el |> tryGetProperty "#text" |> toJsonStringValue
                let! uri = el |> tryGetProperty "@Uri" |> toJsonUriValue UriKind.Relative

                return (DisplayText title, uri)
            }

        elementResult
            |> toResultFromJsonElement
                (fun kind -> kind = JsonValueKind.Array)
                (fun el -> el.EnumerateArray().ToArray())
            >>= fun a ->
                a
                |> List.ofSeq
                |> List.map toPlaylistItem
                |> List.sequenceResultM
                |> Result.map (fun l -> l |> Playlist)

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the root of a legacy Presentation document.
    /// </summary>
    let tryGetPresentationElementResult (json: string) =
        json
        |> tryGetRootElement
        >>= (tryGetProperty <| nameof(Presentation))

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the identifier of a legacy Presentation document.
    /// </summary>
    let tryGetPresentationIdResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty "@ClientId")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the title of a legacy Presentation document.
    /// </summary>
    let tryGetPresentationTitleResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty <| nameof(Title))

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the description of a legacy Presentation document.
    /// </summary>
    let tryGetPresentationDescriptionResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty <| nameof(Description))
        >>= (tryGetProperty "#text")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the credits of a legacy Presentation document.
    /// </summary>
    let tryGetPresentationCreditsResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty <| nameof(Credits))
        >>= (tryGetProperty "#text")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the layout metadata of a legacy Presentation document.
    /// </summary>
    let tryGetLayoutMetadataResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty "LayoutMetadata")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the copyright name of a legacy Presentation document.
    /// </summary>
    let tryGetCopyrightNameResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty <| nameof(Copyright))
        >>= (tryGetProperty "@Name")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the copyright year of a legacy Presentation document.
    /// </summary>
    let tryGetCopyrightYearResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty <| nameof(Copyright))
        >>= (tryGetProperty "@Year")

    /// <summary>
    /// Tries to return a <see cref="JsonElement"/>
    /// representing the playlist of a legacy Presentation document.
    /// </summary>
    let tryGetPlaylistRootResult presentationElementResult =
        presentationElementResult
        >>= (tryGetProperty "ItemGroup")
        >>= (tryGetProperty "Item")

    /// <summary>
    /// Tries to return a <see cref="Presentation"/>
    /// from a JSON <see cref="string"/>.
    /// </summary>
    let tryGetPresentation (json: string) =
        let presentationElementResult = json |> tryGetPresentationElementResult

        result {

            let! id =
                presentationElementResult
                |> tryGetPresentationIdResult
                |> toResultFromStringElement (fun el -> el.GetString() |> Identifier.fromString |> Id)

            and! title =
                presentationElementResult
                |> tryGetPresentationTitleResult
                |> toResultFromStringElement (fun el -> el.GetString() |> Title)

            and! cssVariableAndValues =
                presentationElementResult
                |> tryGetLayoutMetadataResult
                |> toPresentationCssVariablesResult

            and! description =
                presentationElementResult
                |> tryGetPresentationDescriptionResult
                |> toResultFromStringElement (fun el -> el.GetString() |> PresentationDescription)

            and! credits =
                presentationElementResult
                |> tryGetPresentationCreditsResult
                |> toPresentationCreditsResult

            and! copyrights =
                (
                    presentationElementResult |> tryGetCopyrightNameResult,
                    presentationElementResult |> tryGetCopyrightYearResult
                )
                ||> toPresentationCopyrights

            and! playlist =
                presentationElementResult
                |> tryGetPlaylistRootResult
                |> toPresentationPlaylistResult

            return

                {
                    id = id
                    title = title
                    cssVariables = cssVariableAndValues
                    parts = [
                        description
                        credits
                        copyrights
                        playlist
                    ]
                }
        }
