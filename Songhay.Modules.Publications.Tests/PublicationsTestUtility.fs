module Songhay.Modules.Publications.Tests.PublicationsTestUtility

open System
open System.IO
open System.Linq
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open Songhay.Modules.Models

open FsToolkit.ErrorHandling

open Songhay.Modules.ProgramFileUtility

let directoryName (dir: string) = dir.Split(Path.DirectorySeparatorChar).Last()

let jsonSerializerOptions() =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options

let nl = Environment.NewLine

let projectDirectoryInfo =
    Assembly.GetExecutingAssembly()
    |> ProgramAssemblyInfo.getPathFromAssembly "../../../"
    |> Result.valueOr raiseProgramFileError
    |> DirectoryInfo

let getContainerDirectories(containerName: string) =
    result {
        let root = projectDirectoryInfo.Parent.Parent.FullName
        let! path = tryGetCombinedPath root $"azure-storage-accounts/songhaystorage/{containerName}/"

        return Directory.EnumerateDirectories(path)
    }
    |> Result.valueOr raiseProgramFileError

let getJsonDocument (fileName: string) =
    let path =
        $"./json/{fileName}"
        |> tryGetCombinedPath projectDirectoryInfo.FullName
        |> Result.valueOr raiseProgramFileError
    JsonDocument.Parse(File.ReadAllText(path))
