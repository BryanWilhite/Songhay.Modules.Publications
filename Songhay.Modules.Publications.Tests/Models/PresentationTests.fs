namespace Songhay.Modules.Publications.Tests.Models

open Xunit
open Xunit.Abstractions

open FsToolkit.ErrorHandling

open Songhay.Modules.Models
open Songhay.Modules.Publications.Models
open Songhay.Modules.Publications.Tests

type PresentationTests(testOutputHelper: ITestOutputHelper) =

    [<Theory>]
    [<InlineData("my-presentation.json")>]
    member _.``fromInput test``(fileName: string) =
        // arrange:
        testOutputHelper.WriteLine $"loading `{fileName}` from project..."
        let json = getProjectJsonStringFromFileName fileName

        // act:
        let actual = Presentation.fromInput json

        // assert:
        actual
        |> Result.tee(fun ok -> testOutputHelper.WriteLine $"{nameof Presentation}: {ok}")
        |> Result.teeError (fun exn -> testOutputHelper.WriteLine <| wrapErrorMessage exn)
        |> _.IsOk |> Assert.True

    [<Theory>]
    [<InlineData("my-presentation.json", "http://localhost:8080", null)>]
    [<InlineData("my-presentation.json", "http://localhost:8080", "one")>]
    [<InlineData("my-presentation.json", "http://localhost:8080", "one/two")>]
    [<InlineData("my-presentation.json", "http://localhost:8080", "one/two/")>]
    [<InlineData("my-presentation.json", "http://localhost:8080", "/one/two/")>]
    member _.``toPlaylistWithApiBase test``(fileName: string, location: string, path: string) =
        // arrange:
        testOutputHelper.WriteLine $"loading `{fileName}` from project..."
        let pathOption = path |> Option.ofObj
        let json = getProjectJsonStringFromFileName fileName
        let apiBase = ApiBase location

        // act:
        let actual = (
            (Presentation.fromInput json)
            |> Result.valueOr raise).toPlaylistWithApiBase pathOption apiBase

        // assert:
        actual
        |> Option.teeSome(fun l ->
                Assert.All(l, fun (displayText, uri) ->
                    testOutputHelper.WriteLine $"verifying `{displayText}` ({uri.OriginalString})..."
                    uri.IsAbsoluteUri |> Assert.True
                )
            )
        |> _.IsSome |> Assert.True
