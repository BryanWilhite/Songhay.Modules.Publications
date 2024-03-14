namespace Songhay.Modules.Publications.Tests

open System.Text.Json

open Xunit

open FsUnit.Xunit
open FsUnit.CustomMatchers
open FsToolkit.ErrorHandling

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models
open Songhay.Modules.Publications.DisplayItemModelUtility

open Songhay.Modules.Publications.Tests.PublicationsTestUtility

module DisplayItemModelUtilityTests =

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
