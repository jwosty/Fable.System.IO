module Fable.System.IO.File.Tests
open System
#if FABLE_COMPILER
open Fable.Mocha
#else
open System.Runtime.InteropServices
open Expecto
#endif
open Utils

//let mockFileApi (pathMap:  = {
//    new Fable.System.IOImpl.IOApi with
//        member this.ReadAllText ()
//}


let makeIOApi (paths: (string*string) seq) =
    let pathMap = Map.ofSeq paths
    { new Fable.System.IOImpl.IIOApi with
        member this.ReadAllText path = pathMap.[path]
    }

let normalizeNewlines (str: string) = str.Replace ("\r\n", "\n")

[<Tests>]
let Tests =
    //let mockFableImpl = 

    testList "File" [
        testList "ReadAllText" [
            testCase "Simple files" (fun () ->
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files)
                do
                    let actual = file.ReadAllText "foo.txt"
                    Expect.equal actual "This is foo.txt" "foo.txt contents"
            
                do
                    let actual = file.ReadAllText "bar.txt"
                    Expect.equal actual "Hello\nfrom bar.txt" "bar.txt contents"
            )
            testCase "Same simple file with different contents" (fun () ->
                let files =
                    [   "foo.txt", "Greetings from foo.txt"
                    ] |> makeIOApi
                let file = new Fable.System.IOImpl.file(files) 
                do
                    let actual = file.ReadAllText "foo.txt"
                    Expect.equal actual "Greetings from foo.txt" "foo.txt contents"
            )
            testList "Smoke" [
#if FABLE_COMPILER
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
            ]
        ]
    ]

