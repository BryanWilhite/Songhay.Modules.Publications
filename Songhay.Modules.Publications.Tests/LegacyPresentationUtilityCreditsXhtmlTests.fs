namespace Songhay.Modules.Publications.Tests

open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
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
/// Additionally, the `LegacyPresentationUtility` has been moved/archived to this test project,
/// making the `songhay-presentation-credits-xml.ipynb` notes obsolete.
///</remarks>
type LegacyPresentationUtilityCreditsXhtmlTests(outputHelper: ITestOutputHelper) =

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
    let ``credits processing (1): write XHTML dictionary`` (containerName: string) =
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
    let ``credits processing (2): assert all root children are div elements``(containerName: string) =

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

    [<Theory>]
    [<InlineData("player-audio")>]
    [<InlineData("player-video")>]
    let ``presentationCreditsSet test`` (containerName: string) =
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
