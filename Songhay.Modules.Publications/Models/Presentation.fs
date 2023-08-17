namespace Songhay.Modules.Publications.Models

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
        ///<summary>The Presentation <see cref="CssVariableAndValue"/> collection.</summary>
        cssVariables: CssVariableAndValue list
        ///<summary>The Presentation <see cref="PresentationPart"/> collection.</summary>
        parts: PresentationPart list
    }

    static member internal toOption (l: List<'T>) =
        if l.Length > 0 then l |> List.head |> Some else None

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

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = $"{nameof(this.id)}:{this.id.Value.StringValue}; {nameof(this.title)}:{this.title}"
