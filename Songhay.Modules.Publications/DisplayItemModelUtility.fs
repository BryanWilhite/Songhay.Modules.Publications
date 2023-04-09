namespace Songhay.Modules.Publications

open System
open System.Text.Json

open FsToolkit.ErrorHandling

open Songhay.Modules.Models
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.StringUtility

open Songhay.Modules.Publications.Models

///<summary>
/// Utility functions for <see cref="DisplayItemModel" />.
/// </summary>
module DisplayItemModelUtility =

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on the specified element name.
    /// </summary>
    let displayTextResult elementName (element: JsonElement) =
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            element
            |> tryGetProperty name
            |> toResultFromStringElement (fun el -> Some (DisplayText <| el.GetString()))

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Segment.
    /// </summary>
    let defaultSegmentDisplayTextGetter (useCamelCase: bool) (element: JsonElement) =
        let elementName = $"{nameof Segment}{nameof Name}" |> toCamelCaseOrDefault useCamelCase
        (elementName, element) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Document.
    /// </summary>
    let defaultDocumentDisplayTextGetter (useCamelCase: bool) (element: JsonElement) =
        let elementName = $"{nameof Title}" |> toCamelCaseOrDefault useCamelCase
        (elementName, element) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on the specified element name for the <see cref="PublicationItem" /> Fragment.
    /// </summary>
    let defaultFragmentDisplayTextGetter fragmentElementName (useCamelCase: bool) (element: JsonElement) =
            match fragmentElementName with
            | Some name -> ((name |> toCamelCaseOrDefault useCamelCase), element) ||> displayTextResult
            | _ -> Ok None

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the specified <see cref="PublicationItem" />.
    /// </summary>
    let defaultDisplayTextGetter
        (fragmentElementName: string option)
        (itemType: PublicationItem)
        (useCamelCase: bool)
        (element: JsonElement) =
        match itemType with
        | Segment -> (useCamelCase, element) ||> defaultSegmentDisplayTextGetter
        | Document -> (useCamelCase, element) ||> defaultDocumentDisplayTextGetter
        | Fragment -> (fragmentElementName, useCamelCase, element) |||> defaultFragmentDisplayTextGetter

    ///<summary>
    /// Returns <see cref="DisplayItemModel" /> from the <see cref="JsonElement" />
    /// and ‘getter’ lower-order functions based
    /// on conventions around the specified <see cref="PublicationItem" />.
    /// </summary>
    let tryGetDisplayItemModel
        (displayTextGetter: PublicationItem -> bool -> JsonElement -> Result<DisplayText option, JsonException>)
        (resourceIndicatorGetter: (PublicationItem -> bool -> JsonElement -> Result<Uri option, JsonException>) option)
        (itemType: PublicationItem)
        (useCamelCase: bool)
        (element: JsonElement)
        : Result<DisplayItemModel, JsonException> =

        result {

            let! id = (useCamelCase, element) ||> Id.fromInput itemType
            and! name = (useCamelCase, element) ||> Name.fromInput itemType
            and! displayText =  (useCamelCase, element) ||> displayTextGetter itemType
            and! resourceIndicator =
                match resourceIndicatorGetter with
                | Some getter -> ((useCamelCase, element) ||> getter itemType)
                | _ -> Ok None

            return
                {
                    id = id
                    itemName = name.toItemName
                    displayText = displayText
                    resourceIndicator = resourceIndicator
                }
        }
