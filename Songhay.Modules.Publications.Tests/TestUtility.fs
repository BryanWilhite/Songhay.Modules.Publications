[<AutoOpen>]
module Songhay.Modules.Publications.Tests.TestUtility

open System
open System.IO
open System.Linq
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization

open FsToolkit.ErrorHandling

open Songhay.Modules.Models
open Songhay.Modules.ProgramFileUtility

let nl = Environment.NewLine

let projectDirectoryInfo =
    Assembly.GetExecutingAssembly()
    |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
    |> Result.valueOr raiseProgramFileError
    |> DirectoryInfo

let getDirectoryName (dir: string) = dir.Split(Path.DirectorySeparatorChar).Last()

let getJsonSerializerOptions () =
    let options = JsonSerializerOptions()
    options.WriteIndented <- true
    options.Converters.Add(JsonFSharpConverter())
    options

let getStringFromPath (directoryInfo: DirectoryInfo) (path: string) =
    let combinedPath =
        path
        |> tryGetCombinedPath directoryInfo.FullName
        |> Result.valueOr raiseProgramFileError
    combinedPath |> File.ReadAllText

let getProjectJsonStringFromFileName (fileName: string) =
    let path = $"./json/{fileName}"
    path |> getStringFromPath projectDirectoryInfo

let getProjectJsonDocument (fileName: string) =
    (getProjectJsonStringFromFileName fileName) |> JsonDocument.Parse

let wrapErrorMessage (exn: Exception) = $"ERROR: {exn.Message}"

[<Literal>]
let audioContainerName = "player-audio"

[<Literal>]
let videoContainerName = "player-video"

let getContainerDirectories(containerName: string) =
    result {
        let root = projectDirectoryInfo.Parent.Parent.FullName
        let! path = tryGetCombinedPath root $"azure-storage-accounts/songhaystorage/{containerName}/"

        return Directory.EnumerateDirectories(path)
    }
    |> Result.valueOr raiseProgramFileError

let getStorageMirrorPath(containerName: string) (pathFragment: string) =
    result {
        let root = projectDirectoryInfo.Parent.Parent.FullName
        let! path = tryGetCombinedPath root $"azure-storage-accounts/songhaystorage/{containerName}/{pathFragment}"

        return path
    }
    |> Result.valueOr raiseProgramFileError
