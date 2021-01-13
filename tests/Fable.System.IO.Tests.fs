module Fable.System.IO.Tests
open System
#if FABLE_COMPILER
open Fable.Mocha
#else
open System.Runtime.InteropServices
open Expecto
#endif

#if FABLE_COMPILER
type TestsAttribute() = inherit Attribute()
#endif


let arrayToStr (array: 'a[]) =
    "[|" + System.String.Join (";", array) + "|]"

let isWindows = RuntimeInformation.IsOSPlatform OSPlatform.Windows
let osName = if isWindows then "Windows" else "Unix"

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
#if !FABLE_COMPILER
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
#endif
            ]
        ]

    let combineTests =
        let testCases = [
            "1 empty part",
                [|""|],                     "",                 ""
            "2 empty parts",
                [|"";""|],                  "",                 ""
            "3 empty parts",
                [|"";"";""|],               "",                 ""
            "one part",
                [|"foo"|],                  "foo",              "foo"
            "two parts",
                [|"foo";"bar"|],            "foo/bar",          "foo\\bar"
            "three parts",
                [|"foo";"bar";"baz"|],      "foo/bar/baz",      "foo\\bar\\baz"
            "1 part with trailing unix slash",
                [|"foo/"|],                 "foo/",             "foo/"
            "1 part with trailing windows slash",
                [|"foo\\"|],                "foo\\",            "foo\\"
            "3 parts with trailing unix slash",
                [|"foo/"; "bar"; "baz/"|],  "foo/bar/baz/",     "foo/bar\\baz/"
            "3 parts with trailing windows slash",
                [|"foo\\"; "bar"; "baz\\"|],"foo\\bar/baz\\",   "foo\\bar\\baz\\"
            "absolute 3 parts with trailing unix slash",
                [|"C:/"; "foo"; "bar/"|],   "C:/foo/bar/",      "C:/foo\\bar/"
            "absolute 3 parts with trailing windows slash",
                [|"C:\\";"foo";"bar\\"|],   "C:\\foo/bar\\",    "C:\\foo\\bar\\"
            "3 parts with mixed empty & non-empty",
                [|"foo";"";"bar"|],         "foo/bar",          "foo\\bar"
            "3 parts with extra unix slashes",
                [|"foo//";"bar/";"baz/"|],  "foo//bar/baz/",    "foo//bar/baz/"
            "3 parts with extra windows slashes",
                [|"foo\\\\";"bar\\";"baz\\"|], "foo\\\\bar\\baz\\", "foo\\\\bar\\baz\\"
            
            "2 absolute windows paths",
                [|"C:\\foo";"C:\\bar"|],    "C:\\bar",          "C:\\bar"
            "2 absolute unix paths",
                [|"/foo";"/bar"|],          "/bar",             "/bar"
            "mixed windows absolute and relative paths",
                [|"C:\\foo";"bar";"C:\\baz";"qux"|],"C:\\baz/qux",  "C:\\baz\\qux"
            "mixed unix absolute and relative paths",
                [|"/foo";"bar";"/baz";"qux"|],     "/baz/qux",     "/baz\\qux"
        ]
        testList "Combine" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
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
#if !FABLE_COMPILER
                // these tests compare the output to the BCL implementation to verify that they match for a particular
                // platform
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase osName (fun () ->
                            let actual = if isWindows then windowsExpected else unixExpected
                            let expected = global.System.IO.Path.Combine input
                            Expect.equal actual expected ("Path.Combine " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    let directorySeparatorTests = [
        testList "DirectorySeparator" [
#if !FABLE_COMPILER
            testList "OracleTests" [
                testCase osName (fun () ->
                    let actual =
                        if isWindows then
                            Fable.Windows.System.IO.Path.DirectorySeparatorChar
                        else
                            Fable.Unix.System.IO.Path.DirectorySeparatorChar
                    Expect.equal actual global.System.IO.Path.DirectorySeparatorChar
                        ("Path.DirectorySeparatorChar " + osName + " (Oracle)")
                )
            ]
#endif
        ]
        testList "AltDirectorySeparator" [
#if !FABLE_COMPILER
            testList "OracleTests" [
                testCase osName (fun () ->
                    let actual =
                        if isWindows then
                            Fable.Windows.System.IO.Path.AltDirectorySeparatorChar
                        else
                            Fable.Unix.System.IO.Path.AltDirectorySeparatorChar
                    Expect.equal actual global.System.IO.Path.AltDirectorySeparatorChar
                        ("Path.AltDirectorySeparatorChar " + osName + " (Oracle)")
                )
            ]
#endif
        ]
    ]

    testList "Path" [
        yield isPathRootedTests
        yield combineTests
        yield! directorySeparatorTests
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