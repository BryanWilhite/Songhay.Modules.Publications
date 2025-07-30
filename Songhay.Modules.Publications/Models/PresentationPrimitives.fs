namespace Songhay.Modules.Publications.Models

open System

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models

/// <summary>
/// Defines Publication copyright
/// </summary>
type Copyright =
    {
        ///<summary>The Copyright year.</summary>
        year: int
        ///<summary>The Copyright name.</summary>
        name: string
    }

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = $"©{this.year} {this.name}"

/// <summary>
/// Defines Publication description
/// </summary>
type Description =
    ///<summary>Publication description.</summary>
    | Description of DisplayText

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = match this with Description dt -> dt.Value

/// <summary>
/// Defines the Publication document concepts
/// </summary>
type PresentationDocument =
    ///<summary>Identifies a <see cref="string" /> as CSV.</summary>
    | Csv of string
    ///<summary>Identifies a <see cref="string" /> as HTML.</summary>
    | Html of string
    ///<summary>Identifies a <see cref="string" /> as JSON.</summary>
    | Json of string
    ///<summary>Identifies a <see cref="string" /> as plain text demarcated by line breaks.</summary>
    | Lines of string list
    ///<summary>Identifies a <see cref="string" /> as Markdown.</summary>
    | Markdown of string
    ///<summary>Identifies a document with a conventional identifier (<see cref="Uri"/>).</summary>
    | Permalink of Uri
    ///<summary>Identifies a <see cref="string" /> as SVG.</summary>
    | Svg of string
    ///<summary>Identifies a <see cref="string" /> as TTML (Timed Text Markup Language).</summary>
    | Ttml of string
    ///<summary>Identifies a <see cref="string" /> as XHTML.</summary>
    | Xhtml of string
    ///<summary>Identifies a <see cref="string" /> as YAML.</summary>
    | Yaml of string

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() =
        match this with
        | Csv csv -> csv
        | Html html -> html
        | Json json -> json
        | Lines l -> l |> List.reduce (fun a i -> $"{a}{Environment.NewLine}{i}")
        | Markdown md -> md
        | Permalink uri -> uri.OriginalString
        | Svg svg -> svg
        | Ttml ttml -> ttml
        | Xhtml xhtml -> xhtml
        | Yaml yaml -> yaml

/// <summary>
/// Defines Publication credits
/// </summary>
type RoleCredit =
    {
        ///<summary>Publication credits role.</summary>
        role: string
        ///<summary>Publication credits name.</summary>
        name: string
    }

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    override this.ToString() = $"{nameof(this.role)}:{this.role}; {nameof(this.name)}:{this.name}"

/// <summary>
/// Defines Publication stream segments
/// </summary>
type StreamSegment =
    {
        ///<summary>Publication stream segment identifier.</summary>
        id: Id
        ///<summary>Publication stream segment thumbnail.</summary>
        thumbnailUri: Uri
    }

/// <summary>
/// Defines <see cref="Presentation"/> parts
/// </summary>
type PresentationPart =
    ///<summary><see cref="Presentation"/> copyright.</summary>
    | CopyRights of Copyright list
    ///<summary><see cref="Presentation"/> credits.</summary>
    | Credits of RoleCredit list
    ///<summary><see cref="Presentation"/> description.</summary>
    | PresentationDescription of string
    ///<summary><see cref="Presentation"/> pages.</summary>
    | Pages of string list
    ///<summary><see cref="Presentation"/> playlist.</summary>
    | Playlist of (DisplayText * Uri) list
    ///<summary><see cref="Presentation"/> documents.</summary>
    | PresentationDocuments of PresentationDocument list
    ///<summary><see cref="Presentation"/> stream.</summary>
    | Stream of StreamSegment list

    ///<summary>Returns the <see cref="string" /> representation of this instance.</summary>
    member this.StringValue =
        match this with
        | _ -> this.ToString()

    ///<summary>Returns the <see cref="string" /> collection representation of this instance.</summary>
    member this.StringValues =
        match this with
        | CopyRights l -> l |> List.map _.ToString()
        | Pages l -> l
        | Playlist l -> l |> List.map (fun (dt, _) -> dt.Value)
        | PresentationDocuments docs -> docs |> List.map _.ToString()
        | _ -> [this.ToString()]
