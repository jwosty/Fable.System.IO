﻿module Fable.System.IO.File
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
    { new Fable.System.IOImpl.IOApi() with
        member this.AsyncReadAllText path = async { return pathMap.[path] }
        member this.ReadAllText path = pathMap.[path]
    }

let emptyIOApi = makeIOApi []

let normalizeNewlines (str: string) = str.Replace ("\r\n", "\n")

let mkTest (syncFunc: 'inst -> ('arg -> 'c)) (asyncFunc: 'inst -> ('arg -> Async<'c>)) asyncNamePrefixSep syncName (test: _) = [
    testCaseAsync syncName (
        test (fun inst arg -> async { return syncFunc inst arg })
    )
    testCaseAsync ("Async" + asyncNamePrefixSep + syncName) (
        test asyncFunc
    )
]

type f = Fable.System.IOImpl.file

let realFileLines = [| "This is a real file on disk"; "Line 2" |]

[<Tests>]
let tests =
    testList "File" [
        let mkTestReadAllText syncName test = mkTest (fun (f:f) -> f.ReadAllText) (fun (f:f) -> f.AsyncReadAllText) " " syncName test
        testList "ReadAllText" [
            yield! mkTestReadAllText "Simple files" (fun readAllText -> async {
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi)
                let! actual1 = readAllText file "foo.txt"
                Expect.equal actual1 "This is foo.txt" "foo.txt contents"
            
                let! actual2 = readAllText file "bar.txt"
                Expect.equal actual2 "Hello\nfrom bar.txt" "bar.txt contents"
                
            })
            yield! mkTestReadAllText "Same simple file with different contents" (fun readAllText -> async {
                let files =
                    [   "foo.txt", "Greetings from foo.txt" ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi) 
                
                let! actual = readAllText file "foo.txt"
                Expect.equal actual "Greetings from foo.txt" "foo.txt contents"
            })
            yield! mkTestReadAllText "Absolute file path" (fun readAllText -> async {
                let files =
                    [   "C:\\foo\\fruit list.txt", "banana apple pear"
                        "/foo/fruit list.txt", "banana apple pear" // FIXME: this case could potentially be made to work under Fable
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, emptyIOApi)
                let! actual1 = readAllText file "C:\\foo\\fruit list.txt"
                Expect.equal actual1 "banana apple pear" "fruit list.txt"

                let! actual2 = readAllText file "/foo/fruit list.txt"
                Expect.equal actual2 "banana apple pear" "fruit list.txt"
            })
            yield! mkTestReadAllText "Web page contents from absolute URI" (fun readAllText -> async {
                let webFiles =
                    [   "https://example.com/mock-web-data.json", "{ \"data\": \"Hello, world\" }"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file((fun () -> failwith "bang"), webFiles)
                let! actual = readAllText file "https://example.com/mock-web-data.json"
                Expect.equal actual "{ \"data\": \"Hello, world\" }" "mock-web-data.json"
            })
            yield! mkTestReadAllText "Mix of files and web files through absolute paths and URIs" (fun readAllText -> async {
                let files =
                    [   "foo.txt", "This is foo.txt" ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/mock-web-data.json", "{ \"foo\": \"bar\" }"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)

                let! actual1 = readAllText file "https://example.com/mock-web-data.json"
                Expect.equal actual1 "{ \"foo\": \"bar\" }" "mock-web-data.json contents"

                let! actual2 = readAllText file "foo.txt"
                Expect.equal actual2 "This is foo.txt" "foo.txt contents"
            })
            yield! mkTestReadAllText "Relative paths as web files given no file API" (fun readAllText -> async {
                let webFiles =
                    [   "https://example.com/some-mock-data.json", "{ \"foo\": \"bar\" }"
                        "https://example.com/foo/bar/baz.txt", "hello"
                        "https://foo.bar.com/some/file.txt", "Hello, world"
                    ] |> makeIOApi
                let file1 = new Fable.System.IOImpl.file((fun () -> Uri "https://example.com/"), webFiles)

                let! actual1 = readAllText file1 "some-mock-data.json"
                Expect.equal actual1 "{ \"foo\": \"bar\" }" "some-mock-data.json contents"

                let! actual2 = readAllText file1 "foo/bar/baz.txt"
                Expect.equal actual2 "hello" "foo/bar/baz.txt contents"

                let file2 = new Fable.System.IOImpl.file((fun () -> Uri "https://foo.bar.com/index.html"), webFiles)
                let! actual2 = readAllText file2 "some/file.txt"
                Expect.equal actual2 "Hello, world" "some/file.txt contents"

                let file3 = new Fable.System.IOImpl.file((fun () -> Uri "https://foo.bar.com/some/where.html"), webFiles)
                let! actual3 = readAllText file3 "file.txt"
                Expect.equal actual3 "Hello, world" "some/file.txt contents from within some/"

                let file4 = new Fable.System.IOImpl.file((fun () -> Uri "https://foo.bar.com/no/body/knows.html"), webFiles)
                let! actual4 = readAllText file4 "../../some/file.txt"
                Expect.equal actual4 "Hello, world" "'some/file.txt' from within '/no/body/'"
            })
            yield! mkTestReadAllText "Absolute paths as web files relative to the current website" (fun readAllText -> async {
                let webFiles =
                    [   "https://example.com/some/where/over-the-rainbow.txt", "way up high"
                        "https://example.com/no/body/knows.html", "the troubles I've seen"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file((fun () -> Uri "https://example.com/some/"), webFiles)

                let! actual1 = readAllText file "/some/where/over-the-rainbow.txt"
                Expect.equal actual1 "way up high" "/some/where/over-the-rainbow.txt contents"
                let! actual2 = readAllText file "/no/body/knows.html"
                Expect.equal actual2 "the troubles I've seen" "/no/body/knows.html contents"
            })
            testList "Smoke" [
#if !FABLE_COMPILER
                let realFileExpectedContents = String.Join (Environment.NewLine, realFileLines)
                yield! mkTestReadAllText "Real file from relative path" (fun readAllText -> async {
                    let! actual = readAllText Fable.System.IO.File "real-file.txt"
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                })
                yield! mkTestReadAllText "Real file from absolute path" (fun readAllText -> async {
                    let runtimeDir =
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                    let! actual = readAllText Fable.System.IO.File (System.IO.Path.Combine (runtimeDir, "real-file.txt"))
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                })
#endif
                yield! mkTestReadAllText "Real web file from absolute URI" (fun readAllText -> async {
                    let! page = readAllText Fable.System.IO.File "https://www.google.com/"
                    Expect.isTrue (page.Length > 20) "google.com page length > 20 chars"
                })
            ]
        ]
        let mkTestReadAllLines syncName test = mkTest (fun (f:f) -> f.ReadAllLines) (fun (f:f) -> f.AsyncReadAllLines) " " syncName test
        testList "ReadAllLines" [
            yield! mkTestReadAllLines "Unix newlines" (fun readAllLines -> async {
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt\n\nand also line 4"
                    ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/hello.txt", "Hello\nLovely\t world!\n\n"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)
                let! actual1 = readAllLines file "foo.txt"
                Expect.equal actual1 [|"This is foo.txt"|] "foo.txt contents"
            
                let! actual2 = readAllLines file "bar.txt"
                Expect.equal actual2 [|"Hello"; "from bar.txt"; ""; "and also line 4"|] "bar.txt contents"
            
                let! actual3 = readAllLines file "https://example.com/hello.txt"
                Expect.equal actual3 [|"Hello"; "Lovely\t world!"; ""; ""|] "hello.txt web contents"
            })
            yield! mkTestReadAllLines "Windows newlines" (fun readAllLines -> async {
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\r\nfrom bar.txt\r\n\r\nand also line 4"
                    ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/hello.txt", "Hello\r\nLovely\t world!\r\n\r\n"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)
                let! actual1 = readAllLines file "foo.txt"
                Expect.equal actual1 [|"This is foo.txt"|] "foo.txt contents"
            
                let! actual2 = readAllLines file "bar.txt"
                Expect.equal actual2 [|"Hello"; "from bar.txt"; ""; "and also line 4"|] "bar.txt contents"
            
                let! actual3 = readAllLines file "https://example.com/hello.txt"
                Expect.equal actual3 [|"Hello"; "Lovely\t world!"; ""; ""|] "hello.txt web contents"
            })
            testList "Smoke" [
#if !FABLE_COMPILER
                let realFileExpectedContents = realFileLines
                yield! mkTestReadAllLines "Real file from relative path" (fun readAllLines -> async {
                    let! actual = readAllLines Fable.System.IO.File "real-file.txt"
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                })
                yield! mkTestReadAllLines "Real file from absolute path" (fun readAllLines -> async {
                    let runtimeDir =
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                    let! actual = readAllLines Fable.System.IO.File (System.IO.Path.Combine (runtimeDir, "real-file.txt"))
                    Expect.equal actual realFileExpectedContents "real-file.txt contents"
                })
#endif
                yield! mkTestReadAllLines "Real web file from absolute URI" (fun readAllLines -> async {
                    let! page = readAllLines Fable.System.IO.File "https://www.google.com/"
                    Expect.isTrue (page.Length > 5) "google.com page length > 5 lines"
                })
                ]
        ]
        let mkTestReadLines syncName test = [testCase syncName (fun () -> test (fun (f:f) -> f.ReadLines))]
        testList "ReadLines" [
            yield! mkTestReadLines "Unix newlines" (fun readLines ->
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt\n\nand also line 4"
                    ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/hello.txt", "Hello\nLovely\t world!\n\n"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)
                let actual1 = readLines file "foo.txt"
                Expect.sequenceEqual actual1 ["This is foo.txt"] "foo.txt contents"
            
                let actual2 = readLines file "bar.txt"
                Expect.sequenceEqual actual2 ["Hello"; "from bar.txt"; ""; "and also line 4"] "bar.txt contents"
            
                let actual3 = readLines file "https://example.com/hello.txt"
                Expect.sequenceEqual actual3 ["Hello"; "Lovely\t world!"; ""; ""] "hello.txt web contents"
            )
            yield! mkTestReadLines "Windows newlines" (fun readLines ->
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\r\nfrom bar.txt\r\n\r\nand also line 4"
                    ] |> makeIOApi
                let webFiles =
                    [   "https://example.com/hello.txt", "Hello\r\nLovely\t world!\r\n\r\n"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files, webFiles)
                let actual1 = readLines file "foo.txt"
                Expect.sequenceEqual actual1 ["This is foo.txt"] "foo.txt contents"
            
                let actual2 = readLines file "bar.txt"
                Expect.sequenceEqual actual2 ["Hello"; "from bar.txt"; ""; "and also line 4"] "bar.txt contents"
            
                let actual3 = readLines file "https://example.com/hello.txt"
                Expect.sequenceEqual actual3 ["Hello"; "Lovely\t world!"; ""; ""] "hello.txt web contents"
            )
            testList "Smoke" [
#if !FABLE_COMPILER
                let realFileExpectedContents = seq realFileLines
                yield! mkTestReadLines "Real file from relative path" (fun readLines ->
                    let actual = readLines Fable.System.IO.File "real-file.txt"
                    Expect.sequenceEqual actual realFileExpectedContents "real-file.txt contents"
                )
                yield! mkTestReadLines "Real file from absolute path" (fun readLines ->
                    let runtimeDir =
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                    let actual = readLines Fable.System.IO.File (System.IO.Path.Combine (runtimeDir, "real-file.txt"))
                    Expect.sequenceEqual actual realFileExpectedContents "real-file.txt contents"
                )
#endif
                yield! mkTestReadLines "Real web file from absolute URI" (fun readLines ->
                    let page = readLines Fable.System.IO.File "https://www.google.com/"
                    Expect.isTrue (Seq.length page > 5) "google.com page length > 5 lines"
                )
            ]
        ]
    ]

