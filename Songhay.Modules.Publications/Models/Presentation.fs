namespace Songhay.Modules.Publications.Models

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models

type Presentation =
    {
        id: Id
        title: Title
        cssVariables: CssVariableAndValue list
        parts: PresentationPart list
    }

    override this.ToString() = $"{nameof(this.id)}:{this.id.Value.StringValue}; {nameof(this.title)}:{this.title}"
