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

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = $"{nameof(this.id)}:{this.id.Value.StringValue}; {nameof(this.title)}:{this.title}"
