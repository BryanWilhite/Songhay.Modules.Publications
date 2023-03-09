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
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonDocumentOrElement" />
    /// based on the specified element name.
    /// </summary>
    let displayTextResult elementName (documentOrElement: JsonDocumentOrElement) =
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            documentOrElement
            |> tryGetProperty name
            |> Result.map toJsonElement
            |> toResultFromStringElement (fun el -> Some (el.GetString() |> DisplayText))

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonDocumentOrElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Segment.
    /// </summary>
    let defaultSegmentDisplayTextGetter (useCamelCase: bool) (documentOrElement: JsonDocumentOrElement) =
        let elementName = $"{nameof Segment}{nameof Name}" |> toCamelCaseOrDefault useCamelCase
        (elementName, documentOrElement) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonDocumentOrElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Document.
    /// </summary>
    let defaultDocumentDisplayTextGetter (useCamelCase: bool) (documentOrElement: JsonDocumentOrElement) =
        let elementName = $"{nameof Title}" |> toCamelCaseOrDefault useCamelCase
        (elementName, documentOrElement) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonDocumentOrElement" />
    /// based on the specified element name for the <see cref="PublicationItem" /> Fragment.
    /// </summary>
    let defaultFragmentDisplayTextGetter fragmentElementName (useCamelCase: bool) (documentOrElement: JsonDocumentOrElement) =
            match fragmentElementName with
            | Some name -> ((name |> toCamelCaseOrDefault useCamelCase), documentOrElement) ||> displayTextResult
            | _ -> Ok None

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonDocumentOrElement" />
    /// based on conventions around the specified <see cref="PublicationItem" />.
    /// </summary>
    let defaultDisplayTextGetter
        (fragmentElementName: string option)
        (itemType: PublicationItem)
        (useCamelCase: bool)
        (documentOrElement: JsonDocumentOrElement) =
        match itemType with
        | Segment -> (useCamelCase, documentOrElement) ||> defaultSegmentDisplayTextGetter
        | Document -> (useCamelCase, documentOrElement) ||> defaultDocumentDisplayTextGetter
        | Fragment -> (fragmentElementName, useCamelCase, documentOrElement) |||> defaultFragmentDisplayTextGetter

    ///<summary>
    /// Returns <see cref="DisplayItemModel" /> from the <see cref="JsonDocumentOrElement" />
    /// and ‘getter’ lower-order functions based
    /// on conventions around the specified <see cref="PublicationItem" />.
    /// </summary>
    let tryGetDisplayItemModel
        (displayTextGetter: PublicationItem -> bool -> JsonDocumentOrElement -> Result<DisplayText option, JsonException>)
        (resourceIndicatorGetter: (PublicationItem -> bool -> JsonDocumentOrElement -> Result<Uri option, JsonException>) option)
        (itemType: PublicationItem)
        (useCamelCase: bool)
        (documentOrElement: JsonDocumentOrElement)
        : Result<DisplayItemModel, JsonException> =

        let idResult = (useCamelCase, documentOrElement) ||> Id.fromInput itemType
        let nameResult = (useCamelCase, documentOrElement) ||> Name.fromInput itemType
        let displayTextResult =  (useCamelCase, documentOrElement) ||> displayTextGetter itemType
        let resourceIndicatorResult =
            match resourceIndicatorGetter with
            | Some getter -> ((useCamelCase, documentOrElement) ||> getter itemType)
            | _ -> Ok None

        [
            idResult |> Result.map (fun _ -> true)
            nameResult  |> Result.map (fun _ -> true)
            displayTextResult  |> Result.map (fun _ -> true)
            resourceIndicatorResult  |> Result.map (fun _ -> true)
        ]
        |> List.sequenceResultM
        |> Result.either
              (
                   fun _ ->
                        Ok {
                            id = (idResult |> Result.valueOr raise)
                            itemName = (nameResult |> Result.valueOr raise).toItemName
                            displayText = (displayTextResult |> Result.valueOr raise)
                            resourceIndicator = (resourceIndicatorResult |> Result.valueOr raise)
                        }
               )
               Result.Error
