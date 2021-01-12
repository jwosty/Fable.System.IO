module Fable.System.IO.Tests
open System
open System.Runtime.InteropServices
#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

let arrayToStr (array: 'a[]) =
    "[|" + System.String.Join (";", array) + "|]"

let ``Fable.System.IO.Path.Tests`` =
    let combineTests =
        let testCases = [
            [|"foo"|], "foo", "foo"
            [|"foo"; "bar"|], "foo/bar", "foo\\bar"
        ]
        testList "Combine" [
            testList "IndependentTests" [
                for (input, unixExpected, windowsExpected) in testCases ->
                    testList (arrayToStr input) [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.Combine input
                            Expect.equal actual windowsExpected "Path.Combine windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.Combine input
                            Expect.equal actual unixExpected "Path.Combine unix"
                        )
                    ]
            ]
            testList "OracleTests" [
                // these tests compare the output to the BCL implementation to verify that they match for a particular
                // platform
                for (input, unixExpected, windowsExpected) in testCases ->
                    testList (arrayToStr input) [
                        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                            testCase "Windows" (fun () ->
                                let actual = windowsExpected
                                let expected = global.System.IO.Path.Combine input
                                Expect.equal actual expected "Path.Combine Windows (Oracle)"
                            )
                        else
                            testCase "Unix" (fun () ->
                                let actual = unixExpected
                                let expected = global.System.IO.Path.Combine input
                                Expect.equal actual expected "Path.Combine Unix (Oracle)"
                            )
                    ]
            ]
        ]

    testList "Path" [
        combineTests
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