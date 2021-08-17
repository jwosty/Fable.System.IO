module Fable.System.IO.File
open System
#if FABLE_COMPILER
open Fable.Mocha
#else
open System.Runtime.InteropServices
open Expecto
#endif
open Utils


let makeIOApi (paths: (string*string) seq) =
    let pathMap = Map.ofSeq paths
    { new Fable.System.IOImpl.IIOApi with
        member this.ReadAllText path = pathMap.[path]
    }

let emptyIOApi = makeIOApi []

let normalizeNewlines (str: string) = str.Replace ("\r\n", "\n")

[<Tests>]
let tests =
    testList "File" [
        testList "ReadAllText" [
            testCase "Simple files" (fun () ->
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi)
                do
                    let actual = file.ReadAllText "foo.txt"
                    Expect.equal actual "This is foo.txt" "foo.txt contents"
            
                do
                    let actual = file.ReadAllText "bar.txt"
                    Expect.equal actual "Hello\nfrom bar.txt" "bar.txt contents"
            )
            testCase "Same simple file with different contents" (fun () ->
                let files =
                    [   "foo.txt", "Greetings from foo.txt" ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi) 
                do
                    let actual = file.ReadAllText "foo.txt"
                    Expect.equal actual "Greetings from foo.txt" "foo.txt contents"
            )
#if !FABLE_COMPILER
            testCase "Absolute file path" (fun () ->
                let files =
                    [   "C:\\foo\\fruit list.txt", "banana apple pear"
                        "/foo/fruit list.txt", "banana apple pear" // TODO: this case could potentially be made to work under Fable
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi)
                do
                    Expect.equal
                        (file.ReadAllText "C:\\foo\\fruit list.txt")
                        "banana apple pear" "fruit list.txt"

                    Expect.equal
                        (file.ReadAllText "/foo/fruit list.txt")
                        "banana apple pear" "fruit list.txt"
            )
#endif
            testCase "Web page contents from absolute URI" (fun () ->
                let webFiles =
                    [   "https://example.com/mock-web-data.json", "{ \"data\": \"Hello, world\" }"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file((fun () -> failwith "bang"), webFiles)
                let actual = file.ReadAllText "https://example.com/mock-web-data.json"
                Expect.equal actual "{ \"data\": \"Hello, world\" }" "mock-web-data.json"
            )
            testCase "Mix of files and web files through absolute paths and URIs" (fun () ->
                let files =
                    [   "foo.txt", "This is foo.txt" ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/mock-web-data.json", "{ \"foo\": \"bar\" }"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)

                Expect.equal
                    (file.ReadAllText "https://example.com/mock-web-data.json")
                    "{ \"foo\": \"bar\" }"
                    "mock-web-data.json contents"

                Expect.equal
                    (file.ReadAllText "foo.txt")
                    "This is foo.txt"
                    "foo.txt contents"
            )
            testCase "Relative paths as web files given no file API" (fun () ->
                let webFiles =
                    [   "https://example.com/some-mock-data.json", "{ \"foo\": \"bar\" }"
                        "https://example.com/foo/bar/baz.txt", "hello"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file((fun () -> Uri "https://example.com/"), webFiles)

                Expect.equal
                    (file.ReadAllText "some-mock-data.json")
                    "{ \"foo\": \"bar\" }"
                    "some-mock-data.json contents"

                Expect.equal
                    (file.ReadAllText "foo/bar/baz.txt")
                    "hello"
                    "foo/bar/baz.txt contents"
            )
            testCase "Relative path as web file with different base URLs" (fun () ->
                let webFiles =
                    [   "https://foo.bar.com/some/file.txt", "Hello, world"
                    ] |> makeIOApi

                do
                    let file = new Fable.System.IOImpl.file((fun () -> Uri "https://foo.bar.com/index.html"), webFiles)

                    Expect.equal
                        (file.ReadAllText "some/file.txt")
                        "Hello, world"
                        "some/file.txt contents"

                do
                    let file = new Fable.System.IOImpl.file((fun () -> Uri "https://foo.bar.com/some/where.html"), webFiles)

                    Expect.equal
                        (file.ReadAllText "file.txt")
                        "Hello, world"
                        "some/file.txt contents from within some/"
            )
            testList "Smoke" [
#if FABLE_COMPILER
                //testCase "Real web page contents from relative path" (fun () ->
                //    let actual = Fable.System.IO.File.ReadAllText "mock-web-data.json"

                //    Expect.equal actual "{ \"data\": \"Hello, world\" }"
                //)
#else
                let realFileExpectedContents = String.Join (Environment.NewLine, ["This is a real file on disk";"Line 2"])
                testCase "Real file from relative path" (fun () ->
                    let actual = Fable.System.IO.File.ReadAllText "real-file.txt"
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                )
                testCase "Real file from absolute path" (fun () ->
                    let runtimeDir =
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                    let actual = Fable.System.IO.File.ReadAllText (System.IO.Path.Combine (runtimeDir, "real-file.txt"))
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                )
#endif
                testCase "Real web file from absolute URI" (fun () ->
                    let page = Fable.System.IO.File.ReadAllText "https://www.google.com/"
                    printfn "PAGE CONTENTS: %s" page
                    Expect.isTrue (page.Length > 20) "google.com page length > 20 chars"
                )
            ]
        ]
    ]

