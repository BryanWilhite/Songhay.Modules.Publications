namespace Songhay.Modules.Publications.Tests

module DisplayItemModelUtilityTests =

    open System
    open System.IO
    open System.Reflection
    open System.Text.Json

    open Xunit

    open Xunit
    open FsUnit.Xunit
    open FsUnit.CustomMatchers
    open FsToolkit.ErrorHandling

    open Songhay.Modules.Models
    open Songhay.Modules.JsonDocumentUtility
    open Songhay.Modules.ProgramFileUtility
    open Songhay.Modules.Publications.Models
    open Songhay.Modules.Publications.DisplayItemModelUtility

    let projectDirectoryInfo =
        Assembly.GetExecutingAssembly()
        |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
        |> Result.valueOr raiseProgramFileError
        |> DirectoryInfo

    let getJsonDocument (fileName: string) =
        let path =
            $"./json/{fileName}"
            |> tryGetCombinedPath projectDirectoryInfo.FullName
            |> Result.valueOr raiseProgramFileError
        JsonDocument.Parse(File.ReadAllText(path))

    [<Theory>]
    [<InlineData("Segment", true, null,"segment-without-documents.json")>]
    [<InlineData("Document", true, null,"publication-document-frontmatter.json")>]
    let ``tryGetDisplayItemModel test``
        ( itemTypeString: string, shouldUseCamelCase: bool, fragmentElementName: string, fileName: string ) =
        let jsonDocument = fileName |> getJsonDocument
        let itemType = (itemTypeString |> PublicationItem.fromString |> Result.valueOr raise)
        let fragmentElementNameOption = Option.ofObj(fragmentElementName)
        let displayTextGetter = defaultDisplayTextGetter fragmentElementNameOption
        let result =
            (shouldUseCamelCase, jsonDocument.RootElement)
            ||> tryGetDisplayItemModel displayTextGetter None itemType

        result |> should be (ofCase <@ Result<DisplayItemModel, JsonException>.Ok @>)
