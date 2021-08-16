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




[<Tests>]
let Tests =
    //let mockFableImpl = 

    testList "File" [
        testList "ReadAllText" [
            testCase "Simple files" (fun () ->
                let files =
                    [   "foo.txt", "This is foo.txt"
                        "bar.txt", "Hello\nfrom bar.txt"
                    ] |> Map.ofList
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
                    ] |> Map.ofList
                let file = new Fable.System.IOImpl.file(files) 
                do
                    let actual = file.ReadAllText "foo.txt"
                    Expect.equal actual "Greetings from foo.txt" "foo.txt contents"
            )
        ]
        
    ]

