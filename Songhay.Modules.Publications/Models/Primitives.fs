module Songhay.Modules.Publications.Models

open System
open System.Text.Json

open Songhay.Modules.Models
open Songhay.Modules.JsonDocumentUtility
open Songhay.Modules.StringUtility

///<summary>
/// Asserts that the JSON input
/// is shaped like a Publication
/// Segment, Document or Fragment.
/// </summary>
/// <remarks>
/// See the C# Abstractions defining these Publication types.
/// [ https://github.com/BryanWilhite/Songhay.Publications/tree/master/Songhay.Publications/Abstractions ]
/// </remarks>
type PublicationItem =
    | Segment
    | Document
    | Fragment

    ///<summary>
    /// Converts a <see cref="string" /> scalar of either
    /// <c>"segment"</c>, <c> "document"</c> or <c>"fragment"</c>
    /// into <see cref="PublicationItem" />
    /// or results in <see cref="FormatException" />.
    /// </summary>
    static member fromString (s: string) =
        match s.ToLower() with
        | "segment" -> Ok Segment
        | "document" -> Ok Document
        | "fragment" -> Ok Fragment
        | _ -> Error (FormatException("The expected conventional string is not here."))

///<summary>
/// Defines a primitive identifier
/// shared among <see cref="PublicationItem" /> types.
/// </summary>
/// <remarks>
/// This primitive identifier could represent either
/// <c>SegmentId</c>, <c>DocumentId</c> or <c>FragmentId</c>
/// to be composed in, say, some ad-hoc tuple.
/// </remarks>
type Id =
    | Id of Identifier

    ///<summary>
    /// Converts the specified <see cref="JsonElement" />
    /// into <see cref="Id" /> based on the type of <see cref="PublicationItem" />.
    /// </summary>
    static member fromInput (itemType: PublicationItem) (useCamelCase: bool) (element: JsonElement) =
        let elementName =
            match itemType with
            | Segment -> $"{nameof Segment}{nameof Id}" |> toCamelCaseOrDefault useCamelCase
            | Document -> $"{nameof Document}{nameof Id}" |> toCamelCaseOrDefault useCamelCase
            | Fragment -> $"{nameof Fragment}{nameof Id}" |> toCamelCaseOrDefault useCamelCase

        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name -> element |> Identifier.fromInputElementName name

    ///<summary>
    /// Unwraps the underlying <see cref="Identifier" /> value.
    /// </summary>
    member this.Value = let (Id v) = this in v

///<summary>
/// Defines a primitive value,
/// representing a <see cref="PublicationItem" /> Document Title.
/// </summary>
/// <remarks>
/// See the C# Abstractions defining these Publication types.
/// [ https://github.com/BryanWilhite/Songhay.Publications/tree/master/Songhay.Publications/Abstractions ]
/// </remarks>
type Title =
    | Title of string

    ///<summary>
    /// Converts the specified <see cref="JsonElement" /> into <see cref="Title" />.
    /// </summary>
    static member fromInput (useCamelCase: bool) (element: JsonElement) =
        let elementName = (nameof Title) |> toCamelCaseOrDefault useCamelCase
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            element
            |> tryGetProperty name
            |> toResultFromStringElement (fun el -> Title (el.GetString()))

///<summary>
/// Defines a primitive naming concept
/// shared among <see cref="PublicationItem" /> types.
/// </summary>
/// <remarks>
/// This primitive identifier could represent either
/// <c>SegmentName</c>, <c>Document.FileName</c> or <c>FragmentName</c>
/// to be composed in, say, some ad-hoc tuple.
/// </remarks>
type Name =
    | Name of string option

    ///<summary>
    /// Converts the specified <see cref="JsonElement" />
    /// into <see cref="Name" /> based on the type of <see cref="PublicationItem" />.
    /// </summary>
    static member fromInput (itemType: PublicationItem) (useCamelCase: bool) (element: JsonElement) =
        let elementName =
            match itemType with
            | Segment -> $"{nameof Segment}{nameof Name}" |> toCamelCaseOrDefault useCamelCase
            | Document -> $"File{nameof Name}" |> toCamelCaseOrDefault useCamelCase
            | Fragment -> $"{nameof Fragment}{nameof Name}" |> toCamelCaseOrDefault useCamelCase

        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name -> element |> Name.fromInputElementName name

    ///<summary>
    /// Converts the specified <see cref="JsonElement" />
    /// into <see cref="Name" /> based on the expected JSON element name.
    /// </summary>
    static member fromInputElementName elementName (element: JsonElement) =
        element
        |> tryGetProperty elementName
        |> toResultFromStringElement (fun el -> Name (el.GetString() |> Some))

    ///<summary>
    /// Unwraps the underlying <see cref="string" /> value.
    /// </summary>
    member this.Value = let (Name v) = this in v

    ///<summary>
    /// Unwraps the underlying <see cref="ItemName" /> value.
    /// </summary>
    member this.toItemName = this.Value |> Option.map ItemName

///<summary>
/// Defines a primitive value,
/// representing a <see cref="PublicationItem" /> Document Path.
/// </summary>
/// <remarks>
/// See the C# Abstractions defining these Publication types.
/// [ https://github.com/BryanWilhite/Songhay.Publications/tree/master/Songhay.Publications/Abstractions ]
/// </remarks>
type Path =
    | Path of string

    ///<summary>
    /// Converts the specified <see cref="JsonElement" /> into <see cref="Path" />.
    /// </summary>
    static member fromInput (useCamelCase: bool) (element: JsonElement) =
        let elementName = (nameof Path) |> toCamelCaseOrDefault useCamelCase
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            element
            |> tryGetProperty name
            |> toResultFromStringElement (fun el -> Path (el.GetString()))

///<summary>
/// Defines a primitive value,
/// representing a <see cref="PublicationItem" /> Document File Name.
/// </summary>
/// <remarks>
/// See the C# Abstractions defining these Publication types.
/// [ https://github.com/BryanWilhite/Songhay.Publications/tree/master/Songhay.Publications/Abstractions ]
/// </remarks>
type FileName =
    | FileName of string

    ///<summary>
    /// Converts the specified <see cref="JsonElement" /> into <see cref="FileName" />.
    /// </summary>
    static member fromInput (useCamelCase: bool) (element: JsonElement) =
        let elementName = (nameof FileName) |> toCamelCaseOrDefault useCamelCase
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            element
            |> tryGetProperty name
            |> toResultFromStringElement (fun el -> FileName (el.GetString()))

///<summary>
/// Defines a primitive value,
/// representing a <see cref="PublicationItem" /> Active status.
/// </summary>
/// <remarks>
/// See the C# Abstractions defining these Publication types.
/// [ https://github.com/BryanWilhite/Songhay.Publications/tree/master/Songhay.Publications/Abstractions ]
/// </remarks>
type IsActive =
    | IsActive of bool

    ///<summary>
    /// Converts the specified <see cref="JsonElement" /> into <see cref="IsActive" />.
    /// </summary>
    static member fromInput (useCamelCase: bool) (element: JsonElement) =
        let elementName = (nameof IsActive) |> toCamelCaseOrDefault useCamelCase
        match elementName with
        | None -> JsonException("The expected element-name input is not here") |> Error
        | Some name ->
            element
            |> tryGetProperty name
            |> (
                fun result ->
                    match result with
                    | Ok el when
                        el.ValueKind = JsonValueKind.True ||
                        el.ValueKind = JsonValueKind.False -> el.GetBoolean() |> IsActive |> Ok
                    | Ok el -> JsonException($"The expected {nameof(JsonValueKind)} is not here: {el.ValueKind}") |> Error
                    | Error ex -> Error ex
            )
