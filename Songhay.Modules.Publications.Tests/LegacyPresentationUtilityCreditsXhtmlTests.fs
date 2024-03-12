namespace Songhay.Modules.Publications.Tests

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open System.Xml
open System.Xml.Linq

open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open FsUnit.CustomMatchers
open FsUnit.Xunit
open Xunit
open Xunit.Abstractions

open Songhay.Modules.Models
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.Publications.Models
open Songhay.Modules.ProgramFileUtility

open Songhay.Modules.Publications.LegacyPresentationUtilityCreditsXhtml

///<remarks>
/// The historical discussion in https://github.com/BryanWilhite/jupyter-central/blob/master/funkykb/fsharp/json/songhay-presentation-credits-xml.ipynb
/// has been extended to include `player-video` data as well as the `player-audio` data explored previously.
///
/// Additionally, the `LegacyPresentationUtility` has been moved/archived to this test type,
/// making the `songhay-presentation-credits-xml.ipynb` notes obsolete.
///</remarks>
type LegacyPresentationUtilityCreditsXhtmlTests(outputHelper: ITestOutputHelper) =

    let nl = Environment.NewLine

    let projectDirectoryInfo =
        Assembly.GetExecutingAssembly()
        |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
        |> Result.valueOr raiseProgramFileError
        |> DirectoryInfo

    let directoryName (dir: string) = dir.Split(Path.DirectorySeparatorChar).Last()

    let jsonSerializerOptions() =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options

    let getContainerDirectories(containerName: string) =
        result {
            let root = projectDirectoryInfo.Parent.Parent.FullName
            let! path = tryGetCombinedPath root $"azure-storage-accounts/songhaystorage/{containerName}/"

            return Directory.EnumerateDirectories(path)
        }
        |> Result.valueOr raiseProgramFileError

    let presentationCreditsSet(containerName: string) =

        let credits (dir: string) =
            result {
                let fileName = dir |> directoryName
                let! path = tryGetCombinedPath dir $"{fileName}_credits.json"

                let json = File.ReadAllText(path)

                return JsonSerializer.Deserialize<RoleCredit list>(json, jsonSerializerOptions())
            }

        (containerName |> getContainerDirectories).ToDictionary(directoryName, credits)

    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (1): write XHTML dictionary`` (containerName: string) =
        let creditsData = Dictionary()

        containerName
        |> getContainerDirectories
        |> List.ofSeq
        |> List.filter (fun path ->
                let dir = path |> directoryName
                [ "css"; "youtube-channels"; "youtube-uploads" ] |> List.contains dir |> not
            )
        |> List.sort
        |> List.iter
            (
                fun root ->
                    let fileName = root.Split(Path.DirectorySeparatorChar).Last()
                    outputHelper.WriteLine($"loading `{fileName}`...")

                    match tryGetCombinedPath root $"{fileName}.json" with
                    | Ok path ->
                        let json = File.ReadAllText(path)
                        let presentationElementResult = json |> tryGetPresentationElementResult
                        let xhtml =
                            presentationElementResult
                            >>= (tryGetProperty <| nameof(Credits))
                            >>= (tryGetProperty "#text")
                            |> toResultFromStringElement (_.GetString())
                            |> Result.valueOr raise

                        let xDoc = XDocument.Parse($"<credits>{xhtml}</credits>")

                        creditsData.Add(fileName, xDoc.ToString());

                    | _ -> ()
            )

        let outputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"writing `{outputPath}`...")

        let json = JsonSerializer.Serialize(creditsData, jsonSerializerOptions())
        File.WriteAllText(outputPath, json)

    ///<remarks>
    /// credits inspection: all root children are div elements?
    /// We can visually inspect the HTML output above to verify
    /// that the general format of all credits entries is of the form:
    /// <code>
    /// {credits}
    ///      {div}…{/div}
    ///     {div}…{/div}
    ///     {div}…{/div}
    ///     …
    /// {/credits}
    /// </code>
    ///</remarks>
    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (2): assert all root children are div elements``(containerName: string) =

        let inputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let json = File.ReadAllText(inputPath)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        dictionary.SelectMany
            (
                fun pair ->
                    outputHelper.WriteLine $"checking `{pair.Key}`..."
                    let xDoc = XDocument.Parse pair.Value

                    xDoc.Root.Elements().Select(
                        fun el ->
                            if el.Name.LocalName = "div" then None
                            else
                                Some $"{pair.Key}, {el.Name.LocalName}"
                    )
            )
            |> Array.ofSeq
            |> Array.filter (fun x -> x.IsSome)

    ///<remarks>
    /// To locate <see cref="RoleCredit"/> data
    /// We look for the form:
    /// <code>
    /// {credits}
    ///      {div}
    ///          …{strong}…{/strong}
    ///      {/div}
    ///     …
    /// {/credits}
    /// </code>
    /// This <c>…{strong}…{/strong}</c> pattern is important as the assertions here are:
    ///
    /// - the text node before the first strong element maps to <see cref="RoleCredit.role"/>
    /// - the text content of any strong element maps to <see cref="RoleCredit.name"/>
    ///
    ///</remarks>
    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (3): assert locations of RoleCredit data``(containerName: string) =
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

        let isCreditsWithManyChildDivs (credits: XElement) =
            credits.Name.LocalName = "credits" && credits.Elements("div").Count() > 1

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

                if credits |> isCreditsWithManyChildDivs &&
                    credits.Elements("div").All(fun div ->
                        div |> elementContainsBrElements || div |> elementIsEmptyOrWhiteSpace) then

                    credits
                        .Elements("div")
                        .Nodes()
                        .Where(fun n -> n.NodeType = XmlNodeType.Text)
                        .Select(getXText)
                        .Where(isXTextValid).ToArray()

                else if credits |> isCreditsWithManyChildDivs &&
                    credits.Elements("div").First() |> elementContainsBrElements &&
                    credits.Elements("div").Last() |> elementContainsBrElements |> not &&
                    credits.Elements("div").Last() |> elementContainsStrongElements then

                    credits
                        .Elements("div")
                        .Nodes()
                        .Where(fun n -> n.NodeType = XmlNodeType.Text)
                        .Select(getXText)
                        .Where(isXTextValid).ToArray()

                else if credits |> isCreditsWithManyChildDivs &&
                    credits.Elements("div").First() |> elementContainsBrElements then

                    credits
                        .Elements("div")
                        .First()
                        .Nodes()
                        .Where(fun n -> n.NodeType = XmlNodeType.Text)
                        .Select(getXText)
                        .Where(isXTextValid).ToArray()

                else if credits |> isCreditsWithManyChildDivs then
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

        let inputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPath}`...")
        let json = File.ReadAllText(inputPath)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        let extractNameXText (document: XDocument) =
            match document.Root with
            | credits when credits.Name.LocalName = "credits" ->
                if credits |> isCreditsWithManyChildDivs then
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

        dictionary.Select(fun pair -> pair.Key, extractRoleXText(pair.Value |> XDocument.Parse), extractNameXText(pair.Value |> XDocument.Parse))
            |> Array.ofSeq
            |> Array.filter (fun (_, roles, _) -> roles.Any())
            |> Array.map(
                fun (key, roleArray, nameArray) ->
                    let joinRoles = String.Join(nl,(roleArray |> Array.map(_.Value)))
                    let joinNames = String.Join(nl,(nameArray |> Array.map(_.Value)))

                    outputHelper.WriteLine $"{nl}---{nl}`{key}` {nameof RoleCredit}.role:"
                    outputHelper.WriteLine joinRoles

                    outputHelper.WriteLine $"{nl}`{key}` {nameof RoleCredit}.names:"
                    outputHelper.WriteLine joinNames
                )

    [<Theory>]
    [<InlineData("player-video", "blacktronic0,lintonkj0,mpire_dinner,popmusicvideo0,saundra_quarterman,theblues0")>]
    member this.``credits processing (4): write RoleCredit exceptions for hand editing``(containerName: string) (exceptions: string) =

        let inputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let json = File.ReadAllText(inputPath)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        let exceptionsArray = exceptions.Split(",")
        let exceptionsDoc = XDocument.Parse($"<exceptions>{String.Empty}</exceptions>")
        dictionary
            .Where(fun pair -> exceptionsArray.Contains pair.Key)
            |> List.ofSeq
            |> List.iter (fun pair ->
                let xEl = XElement.Parse pair.Value
                xEl.Add(XAttribute(nameof(pair.Key).ToLowerInvariant(), pair.Key))
                exceptionsDoc.Root.Add(xEl)
            )

        let outputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-exceptions.xml" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        File.WriteAllText(outputPath, exceptionsDoc.ToString())

    [<Theory>]
    [<InlineData("player-video")>]
    member this.``credits processing (5): write hand edits back to XHTML dictionary``(containerName: string) =

        let inputPathForDictionary =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPathForDictionary}`...")
        let json = File.ReadAllText(inputPathForDictionary)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        let inputPathForExceptions =
            $"json/{containerName}-presentation-credits-xhtml-set-exceptions.xml" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPathForExceptions}`...")
        let exceptionsDoc = XDocument.Load(inputPathForExceptions)
        exceptionsDoc.Root.Elements()
        |> Array.ofSeq
        |> Array.iter (
                fun el ->
                    let key = el.FirstAttribute.Value
                    let xhtml = el.ToString()
                    dictionary[key] <- xhtml
            )

        outputHelper.WriteLine($"writing `{inputPathForDictionary}`...")
        let json = JsonSerializer.Serialize(dictionary, jsonSerializerOptions())
        File.WriteAllText(inputPathForDictionary, json)

    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``presentationCreditsSet test`` (containerName: string) =
        let boundSet = containerName |> presentationCreditsSet

        boundSet.ToArray()
        |> Array.iter
            (
                fun pair ->
                    outputHelper.WriteLine $"testing `{pair.Key}`..."
                    pair.Value |> should be (ofCase <@ Result<RoleCredit list,ProgramFileError>.Ok @>)
            )

        let outputPath =
            $"json/{containerName}-presentation-credits-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        let json = JsonSerializer.Serialize(boundSet, jsonSerializerOptions())
        File.WriteAllText(outputPath, json)
