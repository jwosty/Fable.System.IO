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
    let isPathRootedTests =
        let testCases = [
            "empty path", "", false
            "simple unix-style relative path", "foo/bar", false
            "simple windows-style relative path", "foo\\bar", false
            "simple mixed-style relative path", "foo/bar\\baz", false

            "explicit current dir", ".", false
            "explicit parent dir", "..", false
            "explicit current dir with trailing unix path separator", "./", false
            "explicit current dir with trailing windows path separator", ".\\", false
            "explicit parent dir with trailing unix path separator", "../", false
            "explicit parent dir with trailing windows path separator", "..\\", false

            "unix root", "/", true
            "simple unix absolute path", "/foo", true
            "simple unix absolute path with trailing separators", "/foo/", true
            "simple unix absolute path with extra beginning separators", "//foo", true
            
            "windows C drive 1 no slash", "C:", true
            "windows C drive 2 slash", "C:\\", true
            "windows C drive subfolder", "C:\\foo", true
            "windows C drive forward slash", "C:/", true
            "windows C drive subfolder no slash", "C:foo", true
            "windows style root no drive letter", "\\", true
            "windows style root subfolder no drive letter", "\\foo", true
            "windows D drive forward slash", "D://", true
            "windows z drive slash", "z:\\", true
            "windows Z drive slash", "Z:\\", true
            "windows a drive slash", "a:\\", true
            "windows A drive slash", "A:\\", true

            "UNC named pipe", "\\.\\pipe\\MyPipe", true
            
            "strange path", "::", false
        ]
        testList "IsPathRooted" [
            testList "IndependentTests" [
                for (caseName, input, expected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.IsPathRooted input
                            Expect.equal actual expected "Path.IsPathRooted Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.IsPathRooted input
                            Expect.equal actual expected "Path.IsPathRooted Unix"
                        )
                    ]
            ]
            testList "OracleTests" [
                for (caseName, input, expected) in testCases ->
                    testList caseName [
                        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                            testCase "Windows" (fun () ->
                                let oracle = global.System.IO.Path.IsPathRooted input
                                Expect.equal expected oracle "Path.Combine Windows (Oracle)"
                            )
                        else
                            testCase "Unix" (fun () ->
                                let oracle = global.System.IO.Path.IsPathRooted input
                                Expect.equal expected oracle "Path.Combine Unix (Oracle)"
                            )
                    ]
            ]
        ]

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
                            Expect.equal actual windowsExpected "Path.Combine Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.Combine input
                            Expect.equal actual unixExpected "Path.Combine Unix"
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
        isPathRootedTests
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