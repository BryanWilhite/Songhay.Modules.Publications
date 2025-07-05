namespace Songhay.Modules.Publications.Models

open System
open System.Text.Json
open System.Text.Json.Serialization

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models

/// <summary>
/// Defines the Publication Presentation
/// </summary>
type Presentation =
    {
        ///<summary>The Presentation identifier.</summary>
        id: Id
        ///<summary>The Presentation title.</summary>
        title: Title
        ///<summary>The Presentation <see cref="CssCustomPropertyAndValue"/> collection.</summary>
        cssCustomPropertiesAndValues: CssCustomPropertyAndValue list
        ///<summary>The Presentation <see cref="CssCustomPropertyAndValue"/> collection.</summary>
        [<Obsolete("Use `cssCustomPropertiesAndValues` instead.")>]
        cssVariables: CssCustomPropertyAndValue list
        ///<summary>The Presentation <see cref="PresentationPart"/> collection.</summary>
        parts: PresentationPart list
    }

    static member internal getJsonOptions() =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())

        options

    static member internal toOption (l: List<'T>) =
        if l.Length > 0 then l |> List.head |> Some else None

    ///<summary>Deserializes the specified input into an instance of <see cref="Presentation"/>.</summary>
    static member fromInput (json: string) =
        try
            JsonSerializer.Deserialize<Presentation>(json, Presentation.getJsonOptions()) |> Ok
        with
            | :? JsonException as ex -> ex |> Error
            | :? ArgumentNullException -> JsonException "the expected JSON input is not here" |> Error
            | :? NotSupportedException as ex -> JsonException("the JSON input is not supported (see inner exception)", ex) |> Error

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = $"{nameof(this.id)}:{this.id.Value.StringValue}; {nameof(this.title)}:{this.title}"

    ///<summary>Reduces <see cref="Presentation.parts" /> to the list of <see cref="Credits"/>.</summary>
    member this.credits =
        this.parts
            |> List.choose (function | PresentationPart.Credits l -> Some l | _ -> None)
            |> Presentation.toOption

    ///<summary>Reduces <see cref="Presentation.parts" /> to the <see cref="string"/> of <see cref="PresentationDescription"/>.</summary>
    member this.description =
        this.parts
            |> List.choose (function | PresentationPart.PresentationDescription s -> Some s | _ -> None)
            |> Presentation.toOption

    ///<summary>Reduces <see cref="Presentation.parts" /> to the tuple of <see cref="Playlist"/>.</summary>
    member this.playList =
        this.parts
            |> List.choose (function | PresentationPart.Playlist pl -> pl |> Some | _ -> None)
            |> Presentation.toOption

    ///<summary>Serializes this instance of <see cref="Presentation"/> to JSON.</summary>
    member this.toJson (writeIndented: bool) =
        let options = Presentation.getJsonOptions()
        options.WriteIndented <- writeIndented
        JsonSerializer.Serialize(this, options)
