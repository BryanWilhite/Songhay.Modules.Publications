namespace Songhay.Modules.Publications.Tests

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.Json
open System.Xml.Linq

open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open FsUnit.CustomMatchers
open FsUnit.Xunit
open Xunit
open Xunit.Abstractions

open Songhay.Modules.ProgramFileUtility
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.Publications.Models

open Songhay.Modules.Publications.LegacyPresentationUtility
open Songhay.Modules.Publications.LegacyPresentationCreditsXhtmlUtility

open Songhay.Modules.Publications.Tests.PublicationsTestUtility

///<remarks>
/// The historical discussion in https://github.com/BryanWilhite/jupyter-central/blob/master/funkykb/fsharp/json/songhay-presentation-credits-xml.ipynb
/// has been extended to include `player-video` data as well as the `player-audio` data explored previously.
///
/// Additionally, the `LegacyPresentationUtility` has been moved/archived to this test type,
/// making the `songhay-presentation-credits-xml.ipynb` notes obsolete.
///</remarks>
type LegacyPresentationCreditsXhtmlUtilityTests(outputHelper: ITestOutputHelper) =

    [<Literal>]
    let SkipMassRun = true

    [<Literal>]
    let SkipReason = "This method should not run automatically for general test coverage."

    [<SkippableTheory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (1): write XHTML dictionary`` (containerName: string) =
        Skip.If(SkipMassRun, SkipReason)

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
                            |> toResultFromStringElement _.GetString()
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
    [<SkippableTheory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (2): assert all root children are div elements``(containerName: string) =
        Skip.If(SkipMassRun, SkipReason)

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
            |> Array.filter _.IsSome

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
    [<SkippableTheory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``credits processing (3): assert locations of RoleCredit data``(containerName: string) =
        Skip.If(SkipMassRun, SkipReason)

        let inputPath =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPath}`...")
        let json = File.ReadAllText(inputPath)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        dictionary.Select(
            fun pair ->
                let xDoc = pair.Value |> XDocument.Parse
                pair.Key, extractRoleXText(xDoc), extractNameXText(xDoc)
            )
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

    [<SkippableTheory>]
    [<InlineData("player-video", "blacktronic0,lintonkj0,mpire_dinner,popmusicvideo0,saundra_quarterman,theblues0", 1)>]
    [<InlineData("player-video", "dick_gregory0,reel_black00,sekou_sundiata0,sha_cage,shiva,tayari_jones0", 2)>]
    member this.``credits processing (4): write RoleCredit exceptions for hand editing``(containerName: string) (exceptions: string) (editSessionNo: int) =
        Skip.If(SkipMassRun, SkipReason)

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
            $"xml/{containerName}-presentation-credits-xhtml-set-exceptions-{editSessionNo}.xml" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        File.WriteAllText(outputPath, exceptionsDoc.ToString())

    [<SkippableTheory>]
    [<InlineData("player-video", 1)>]
    [<InlineData("player-video", 2)>]
    member this.``credits processing (5): write hand edits back to XHTML dictionary``(containerName: string) (editSessionNo: int) =
        Skip.If(SkipMassRun, SkipReason)


        let inputPathForDictionary =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPathForDictionary}`...")
        let json = File.ReadAllText(inputPathForDictionary)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        let inputPathForExceptions =
            $"xml/{containerName}-presentation-credits-xhtml-set-exceptions-{editSessionNo}.xml" 
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

    [<SkippableTheory>]
    [<InlineData("player-video")>]
    member this.``credits processing (6): write credits data to container mirror``(containerName: string) =
        Skip.If(SkipMassRun, SkipReason)


        let toRoleCreditJson (key: string, roles: XText array, names: XText array) =
            let rolesMapped =
                match key with
                | "arundhati_roy0" | "wm3" ->
                    //the first two names share the same role
                    roles |> Array.insertAt 1 (roles |> Array.head)
                | "ward_churchill0" ->
                    //the first name was captured in role data because `strong` tags are missing
                    roles |> Array.mapi (fun i xText ->
                            if i = 0 then
                                xText.Value <- xText.Value.Replace("Maria Gilardin of ", String.Empty)
                                xText
                            else
                                xText
                        )
                | "daniel_pauly0" ->
                    // one of the roles is empty
                    roles |> Array.choose(fun role -> if role.Value.Length > 1 then Some role else None)
                | _ -> roles

            let namesMapped =
                match key with
                | "ward_churchill0" ->
                    //the first name was captured in role data because `strong` tags are missing
                    names |> Array.insertAt 1 (XText "Bryan Wilhite")
                | _ -> names

            outputHelper.WriteLine($"zipping roles and names for `{key}`...")
            Assert.Equal(rolesMapped.Length, namesMapped.Length)

            let credits =
                Array.zip rolesMapped namesMapped
                |> Array.map
                    (
                        fun (role, name) ->
                            let roleValue =
                                role.Value
                                    .Replace("by", String.Empty)
                                    .Replace(".", String.Empty)
                                    .Replace(",", String.Empty)
                                    .Replace("(", String.Empty)
                                    .Replace("“", String.Empty)
                                    .TrimEnd()

                            let nameValue =
                                name.Value
                                    .Replace(",", String.Empty)
                                    .Replace("(", String.Empty)
                                    .Replace("“", String.Empty)
                                    .TrimEnd()

                            $"{{ \"role\": \"{roleValue}\", \"name\": \"{nameValue}\" }}"
                    )
                |> Array.reduce (fun a s -> $"{a},{nl}{s}")
            $"[{credits}]"

        let creditsDataExport (data: Dictionary<string, string>) =
            data
                .Select(
                    fun pair ->
                        let xDoc = pair.Value |> XDocument.Parse
                        pair.Key, extractRoleXText(xDoc), extractNameXText(xDoc)
                )
                .Where(fun (_, roles, _) -> roles.Any())
                .ToDictionary((fun(key, _, _) -> key), toRoleCreditJson)

        let inputPathForDictionary =
            $"json/{containerName}-presentation-credits-xhtml-set-output.json" 
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError

        outputHelper.WriteLine($"reading `{inputPathForDictionary}`...")
        let json = File.ReadAllText(inputPathForDictionary)
        let dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
        Assert.NotNull dictionary

        let exportDictionary = dictionary |> creditsDataExport

        containerName
        |> getContainerDirectories
        |> List.ofSeq
        |> List.filter (fun path ->
                let dir = path |> directoryName
                [ "css"; "youtube-channels"; "youtube-uploads" ] |> List.contains dir |> not
            )
        |> List.iter (
                fun root ->
                    let fileName = root.Split(Path.DirectorySeparatorChar).Last()
                    match tryGetCombinedPath root $"{fileName}_credits.json" with
                    | Ok path ->
                        if exportDictionary.ContainsKey fileName then
                            let json = exportDictionary[fileName]
                            File.WriteAllText(path, json)
                        else
                            outputHelper.WriteLine $"WARNING `{fileName}` was excluded from {nameof exportDictionary}."

                    | _ -> ()
            )

    [<SkippableTheory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    member this.``presentationCreditsSet test`` (containerName: string) =
        Skip.If(SkipMassRun, SkipReason)

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
