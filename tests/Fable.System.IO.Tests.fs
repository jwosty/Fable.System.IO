module Fable.System.IO.Tests
open System
#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

let arrayToStr (array: 'a[]) =
    "[|" + System.String.Join (";", array) + "|]"

let ``Fable.System.IO.Path.Tests`` =
    testList "Path" [
        testList "Combine" [
            let testCases = [
                [|"foo"|], "foo", "foo"
                [|"foo"; "bar"|], "foo/bar", "foo\\bar"
            ]

            for (input, unixExpected, windowsExpected) in testCases ->
                testList (arrayToStr input) [
                    testCase "Windows" (fun () ->
                        let actual = Fable.Unix.System.IO.Path.Combine input
                        Expect.equal actual unixExpected "Path.Combine unix"
                    )
                    testCase "Unix" (fun () ->
                        let actual = Fable.Windows.System.IO.Path.Combine input
                        Expect.equal actual windowsExpected "Path.Combine windows"
                    )
                ]
        ]
    ]

[<Tests>]
let testSuite =
    testList "Fable" [
        ``Fable.System.IO.Path.Tests``
    ]

[<EntryPoint>]
let main args =
#if FABLE_COMPILER
    Mocha.runTests testSuite
#else
    runTestsWithArgs defaultConfig args testSuite
#endif