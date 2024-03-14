namespace Songhay.Modules.Publications

open System
open System.IO
open System.Linq
open System.Text.Json
open System.Xml
open System.Xml.Linq

open FsToolkit.ErrorHandling

open Songhay.Modules.Publications.Models
open Songhay.Modules.ProgramFileUtility

open Songhay.Modules.Publications.Tests.PublicationsTestUtility

module LegacyPresentationCreditsXhtmlUtility =

    let presentationCreditsSet(containerName: string) =

        let credits (dir: string) =
            result {
                let fileName = dir |> directoryName
                let! path = tryGetCombinedPath dir $"{fileName}_credits.json"

                let json = File.ReadAllText(path)

                return JsonSerializer.Deserialize<RoleCredit list>(json, jsonSerializerOptions())
            }

        (
            containerName
            |> getContainerDirectories
            |> List.ofSeq
            |> List.filter (fun path ->
                    let dir = path |> directoryName
                    [ "css"; "youtube-channels"; "youtube-uploads" ] |> List.contains dir |> not
                )
        ).ToDictionary(directoryName, credits)
    let elementContainsBrElements (element: XElement) =
        element.Elements("br").Any()

    let elementContainsStrongElements (element: XElement) =
        element.Elements("strong").Any()

    let elementFirstNodeIsBr (element: XElement) =
        match element.FirstNode with
        | :? XElement as el when el.Name.LocalName = "br" -> true
        | _ -> false

    let elementFirstNodeIsXText (element: XElement) =
        element.FirstNode.NodeType = XmlNodeType.Text

    let elementIsEmptyOrWhiteSpace (element: XElement) =
        if element.Nodes().Count() = 1 then
            match element.FirstNode with
            | :? XText as txt when String.IsNullOrWhiteSpace(txt.Value) -> true
            | _ -> false
        else false

    let getXText (n: XNode) =
        match n with
        | :? XText as txt when not(String.IsNullOrWhiteSpace(txt.Value)) -> txt
        | :? XElement as el when el.Name.LocalName = "a" || el.Name.LocalName = "font" ->
            match el.Nodes().FirstOrDefault() with
            | :? XText as txt when not(String.IsNullOrWhiteSpace(txt.Value)) -> txt
            | _ -> null
        | _ -> null

    let isCreditsWithOneOrMoreChildDivs (credits: XElement) =
        credits.Name.LocalName = "credits" && credits.Elements("div").Count() >= 1

    let isCreditsWithOneChildDiv (credits: XElement) =
        credits.Name.LocalName = "credits" && credits.Elements("div").Count() = 1

    let isXTextValid (txt: XText) =
        not(txt = null)
        && not(txt.Value.Trim() = "and")
        && not(txt.Value.Trim() = "(")
        && not(txt.Value.Trim() = ")")

    let extractRoleXText (document: XDocument) =
        match document.Root with
        | credits when credits.Name.LocalName = "credits" ->

            if credits |> isCreditsWithOneOrMoreChildDivs &&
                credits.Elements("div").All(fun div ->
                    div |> elementContainsBrElements || div |> elementIsEmptyOrWhiteSpace) then

                credits
                    .Elements("div")
                    .Nodes()
                    .Where(fun n -> n.NodeType = XmlNodeType.Text)
                    .Select(getXText)
                    .Where(isXTextValid).ToArray()

            else if credits |> isCreditsWithOneOrMoreChildDivs &&
                credits.Elements("div").First() |> elementContainsBrElements &&
                credits.Elements("div").Last() |> elementContainsBrElements |> not &&
                credits.Elements("div").Last() |> elementContainsStrongElements then

                credits
                    .Elements("div")
                    .Nodes()
                    .Where(fun n -> n.NodeType = XmlNodeType.Text)
                    .Select(getXText)
                    .Where(isXTextValid).ToArray()

            else if credits |> isCreditsWithOneOrMoreChildDivs &&
                credits.Elements("div").First() |> elementContainsBrElements then

                credits
                    .Elements("div")
                    .First()
                    .Nodes()
                    .Where(fun n -> n.NodeType = XmlNodeType.Text)
                    .Select(getXText)
                    .Where(isXTextValid).ToArray()

            else if credits |> isCreditsWithOneOrMoreChildDivs then
                credits
                    .Elements("div")
                    .Select(fun div ->
                        if div |> elementFirstNodeIsXText then
                            div.FirstNode |> getXText
                        else null
                    )
                    .Where(isXTextValid).ToArray()

            else if
                credits |> isCreditsWithOneChildDiv &&
                credits.Elements("div").First() |> elementContainsBrElements then

                credits
                    .Elements("div")
                    .First()
                    .Nodes()
                    .Where(fun n -> n.NodeType = XmlNodeType.Text)
                    .Select(getXText)
                    .Where(isXTextValid).ToArray()

            else if credits |> elementFirstNodeIsXText then
                credits
                    .Nodes()
                    .Where(fun n -> n.NodeType = XmlNodeType.Text)
                    .Select(getXText)
                    .Where(isXTextValid).ToArray()

            else Array.Empty()
        | _ -> Array.Empty()

    let extractNameXText (document: XDocument) =
        match document.Root with
        | credits when credits.Name.LocalName = "credits" ->
            if credits |> isCreditsWithOneOrMoreChildDivs then
                credits
                    .Elements("div")
                    .SelectMany(fun div ->
                        if div |> elementFirstNodeIsXText || div |> elementFirstNodeIsBr then
                            div
                                .Descendants("strong")
                                .Where(fun strong -> strong.Nodes().Any())
                                .Select(fun strong -> strong.FirstNode |> getXText )
                        else Array.Empty()
                    ).Where(isXTextValid).ToArray()

            else if
                credits |> isCreditsWithOneChildDiv &&
                credits.Elements("div").First() |> elementContainsBrElements then

                credits
                    .Elements("div")
                    .First()
                    .Descendants("strong")
                    .Where(fun strong -> strong.Nodes().Any())
                    .Select(fun strong -> strong.FirstNode |> getXText)
                    .Where(isXTextValid).ToArray()

            else if credits |> elementFirstNodeIsXText then
                credits
                    .Descendants("strong")
                    .Where(fun strong -> strong.Nodes().Any())
                    .Select(fun strong -> strong.FirstNode |> getXText)
                    .Where(isXTextValid).ToArray()

            else Array.Empty()
        | _ -> Array.Empty()
