namespace Songhay.Modules.Publications.Models

open Songhay.Modules.StringUtility

type CssValue =
    | CssValue of string

    member this.Value = let (CssValue v) = this in v

    override this.ToString() = this.Value

type CssVariable =
    | CssVariable of string

    static member fromInput (input: string) =
        match input.TrimStart('-') |> toKabobCase with
        | Some s -> CssVariable $"--{s}"
        | None -> CssVariable "--?"

    member this.toCssPropertyValue = $"var({this.Value})"

    member this.toCssPropertyValueWithFallback (fallback: string) = $"var({this.Value}, {fallback})"

    member this.Value = let (CssVariable v) = this in v

    override this.ToString() = this.Value

type CssVariables =
    | CssVariables of CssVariable list

    static member fromInput (input: string list) = input |> List.map CssVariable.fromInput

type CssVariableAndValue =
    | CssVariableAndValue of CssVariable * CssValue

    member this.Pair = let (CssVariableAndValue (cssVar, cssVal)) = this in (cssVar, cssVal)

    member this.toCssDeclaration = match this with | CssVariableAndValue (cssVar, cssVal) -> $"{cssVar}: {cssVal};"

type CssVariableAndValues = CssVariableAndValue list
