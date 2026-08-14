namespace Songhay.Modules.Publications.Tests

open Xunit

open FsToolkit.ErrorHandling

open Songhay.Modules.Publications.Models
open Songhay.Modules.Publications.DisplayItemModelUtility

open Songhay.Modules.Publications.Tests.TestUtility

module DisplayItemModelUtilityTests =

    [<Theory>]
    [<InlineData("Segment", true, null,"segment-without-documents.json")>]
    [<InlineData("Document", true, null,"publication-document-frontmatter.json")>]
    let ``tryGetDisplayItemModel test``
        ( itemTypeString: string, shouldUseCamelCase: bool, fragmentElementName: string, fileName: string ) =
        let jsonDocument = fileName |> getProjectJsonDocument
        let itemType = (itemTypeString |> PublicationItem.fromString |> Result.valueOr raise)
        let fragmentElementNameOption = Option.ofObj(fragmentElementName)
        let displayTextGetter = defaultDisplayTextGetter fragmentElementNameOption
        let result =
            (shouldUseCamelCase, jsonDocument.RootElement)
            ||> tryGetDisplayItemModel displayTextGetter None itemType

        result.IsOk |> Assert.True
