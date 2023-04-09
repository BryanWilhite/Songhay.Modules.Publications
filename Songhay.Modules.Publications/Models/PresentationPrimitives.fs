namespace Songhay.Modules.Publications.Models

open System

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models

type Copyright =
    {
        year: int
        name: string
    }

    override this.ToString() = $"©{this.year} {this.name}"

type Description =
    | Description of DisplayText

    override this.ToString() = match this with Description dt -> dt.Value

type RoleCredit =
    {
        role: string
        name: string
    }

    override this.ToString() = $"{nameof(this.role)}:{this.role}; {nameof(this.name)}:{this.name}"

type StreamSegment =
    {
        id: Id
        thumbnailUri: Uri
    }

type PresentationPart =
    | CopyRights of Copyright list
    | Credits of RoleCredit list
    | PresentationDescription of string
    | Pages of string list
    | Playlist of (DisplayText * Uri) list
    | Stream of StreamSegment list

    member this.StringValue =
        match this with
        | _ -> this.ToString()

    member this.StringValues =
        match this with
        | CopyRights l -> l |> List.map (fun i -> i.ToString())
        | Pages l -> l
        | Playlist l -> l |> List.map (fun (dt, _) -> dt.Value)
        | _ -> [this.ToString()]
