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
    let displayTextResult elementName (jsonElement: JsonElement) =
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            jsonElement |> tryGetProperty name |> Result.map (fun el -> Some (el.GetString() |> DisplayText))

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Segment.
    /// </summary>
    let defaultSegmentDisplayTextGetter (useCamelCase: bool) (jsonElement: JsonElement) =
        let elementName = $"{nameof Segment}{nameof Name}" |> toCamelCaseOrDefault useCamelCase
        (elementName, jsonElement) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the <see cref="PublicationItem" /> Document.
    /// </summary>
    let defaultDocumentDisplayTextGetter (useCamelCase: bool) (jsonElement: JsonElement) =
        let elementName = $"{nameof Title}" |> toCamelCaseOrDefault useCamelCase
        (elementName, jsonElement) ||> displayTextResult

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on the specified element name for the <see cref="PublicationItem" /> Fragment.
    /// </summary>
    let defaultFragmentDisplayTextGetter fragmentElementName (useCamelCase: bool) (jsonElement: JsonElement) =
            match fragmentElementName with
            | Some name -> ((name |> toCamelCaseOrDefault useCamelCase), jsonElement) ||> displayTextResult
            | _ -> Ok None

    ///<summary>
    /// Returns <see cref="DisplayText" /> from the <see cref="JsonElement" />
    /// based on conventions around the specified <see cref="PublicationItem" />.
    /// </summary>
    let defaultDisplayTextGetter
        (fragmentElementName: string option)
        (itemType: PublicationItem)
        (useCamelCase: bool)
        (jsonElement: JsonElement) =
        match itemType with
        | Segment -> (useCamelCase, jsonElement) ||> defaultSegmentDisplayTextGetter
        | Document -> (useCamelCase, jsonElement) ||> defaultDocumentDisplayTextGetter
        | Fragment -> (fragmentElementName, useCamelCase, jsonElement) |||> defaultFragmentDisplayTextGetter

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

        let idResult = (useCamelCase, element) ||> Id.fromInput itemType
        let nameResult = (useCamelCase, element) ||> Name.fromInput itemType
        let displayTextResult =  (useCamelCase, element) ||> displayTextGetter itemType
        let resourceIndicatorResult =
            match resourceIndicatorGetter with
            | Some getter -> ((useCamelCase, element) ||> getter itemType)
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
                            id = (idResult |> Result.valueOr raise).Value
                            itemName = (nameResult |> Result.valueOr raise).toItemName
                            displayText = (displayTextResult |> Result.valueOr raise)
                            resourceIndicator = (resourceIndicatorResult |> Result.valueOr raise)
                        }
               )
               Result.Error
