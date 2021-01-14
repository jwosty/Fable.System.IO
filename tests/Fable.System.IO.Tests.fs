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

#if !FABLE_COMPILER
let isWindows = RuntimeInformation.IsOSPlatform OSPlatform.Windows
let osName = if isWindows then "Windows" else "Unix"
#endif

let ``Fable.System.IO.Path.Tests`` =
    let isPathRootedTests =
        let testCases = [
            "empty path", "", false, false
            "simple unix-style relative path", "foo/bar", false, false
            "simple windows-style relative path", "foo\\bar", false, false
            "simple mixed-style relative path", "foo/bar\\baz", false, false

            "explicit current dir", ".", false, false
            "explicit parent dir", "..", false, false
            "explicit current dir with trailing unix path separator", "./", false, false
            "explicit current dir with trailing windows path separator", ".\\", false, false
            "explicit parent dir with trailing unix path separator", "../", false, false
            "explicit parent dir with trailing windows path separator", "..\\", false, false

            "unix root", "/", true, true
            "simple unix absolute path", "/foo", true, true
            "simple unix absolute path with trailing separators", "/foo/", true, true
            "simple unix absolute path with extra beginning separators", "//foo", true, true
            
            // Path.IsRootedPath on Unix doesn't think Windows root paths are rooted because it doesn't recognize the
            // Windows directory separator. Hence, these implementations will disagree here.
            "windows C drive 1 no slash", "C:", false, true
            "windows C drive 2 slash", "C:\\", false, true
            "windows C drive subfolder", "C:\\foo", false, true
            "windows C drive forward slash", "C:/", false, true
            "windows C drive subfolder no slash", "C:foo", false, true
            "windows style root no drive letter", "\\", false, true
            "windows style root subfolder no drive letter", "\\foo", false, true
            "windows D drive forward slash", "D://", false, true
            "windows z drive slash", "z:\\", false, true
            "windows Z drive slash", "Z:\\", false, true
            "windows a drive slash", "a:\\", false, true
            "windows A drive slash", "A:\\", false, true

            "UNC named pipe", "\\.\\pipe\\MyPipe", false, true
            
            "strange path", "::", false, false
        ]
        testList "IsPathRooted" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.IsPathRooted input
                            Expect.equal actual windowsExpected "Path.IsPathRooted Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.IsPathRooted input
                            Expect.equal actual unixExpected "Path.IsPathRooted Unix"
                        )
                    ]
            ]
            testList "OracleTests" [
#if !FABLE_COMPILER
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                            testCase "Windows" (fun () ->
                                let oracle = global.System.IO.Path.IsPathRooted input
                                Expect.equal windowsExpected oracle "Path.Combine Windows (Oracle)"
                            )
                        else
                            testCase "Unix" (fun () ->
                                let oracle = global.System.IO.Path.IsPathRooted input
                                Expect.equal unixExpected oracle "Path.Combine Unix (Oracle)"
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
                [|"foo\\"; "bar"; "baz\\"|],"foo\\/bar/baz\\",  "foo\\bar\\baz\\"
            "absolute 3 parts with trailing unix slash",
                [|"C:/"; "foo"; "bar/"|],   "C:/foo/bar/",      "C:/foo\\bar/"
            "absolute 3 parts with trailing windows slash",
                [|"C:\\";"foo";"bar\\"|],   "C:\\/foo/bar\\",   "C:\\foo\\bar\\"
            "3 parts with mixed empty & non-empty",
                [|"foo";"";"bar"|],         "foo/bar",          "foo\\bar"
            "3 parts with extra unix slashes",
                [|"foo//";"bar/";"baz/"|],  "foo//bar/baz/",    "foo//bar/baz/"
            "3 parts with extra windows slashes",
                [|"foo\\\\";"bar\\";"baz\\"|], "foo\\\\/bar\\/baz\\", "foo\\\\bar\\baz\\"
            
            // unix acts funny on some of these because it doesn't recognize Windows drive paths as being rooted
            "2 absolute windows paths",
                [|"C:\\foo";"C:\\bar"|],    "C:\\foo/C:\\bar",  "C:\\bar"
            "2 absolute unix paths",
                [|"/foo";"/bar"|],          "/bar",             "/bar"
            "mixed windows absolute and relative paths",
                [|"C:\\foo";"bar";"C:\\baz";"qux"|],"C:\\foo/bar/C:\\baz/qux",  "C:\\baz\\qux"
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
    
    let getRelativePathTests =
        testList "GetRelativePath" [
            let testCases = [
                // note that in all of these it ends up replacing the path separators with the current platform's
                // separators -- this is exactly how mscorlib System.IO.Path behaves!
                "identical paths",
                    ("SomePath", "SomePath"),       ".",                "."
                "equivalent (but not identical) paths using unix dir sep",
                    ("SomePath", "SomePath/"),      ".",                "."
                "equivalent (but not identical) paths using windows dir sep",
                    ("SomePath", "SomePath\\"),     "../SomePath\\",    "."
                "sibling paths",
                    ("foo", "bar"),                 "../bar",           "..\\bar"
                "1-deep unix-style subdir",
                    ("foo", "foo/bar"),             "bar",              "bar"
                "1-deep windows-style subdir",
                    ("foo", "foo\\bar"),            "../foo\\bar",      "bar"
                "1-deep unix-style subdir in deeper parent",
                    ("foo/bar", "foo/bar/baz"),     "baz",              "baz"
                "1-deep windows-style subdir in deeper parent",
                    ("foo\\bar", "foo\\bar\\baz"),  "../foo\\bar\\baz", "baz"
                "2-deep unix-style subdir",
                    ("foo", "foo/bar/baz"),         "bar/baz",          "bar\\baz"
                "2-deep windows-style subdir",
                    ("foo", "foo\\bar\\baz"),       "../foo\\bar\\baz", "bar\\baz"
                "2-deep unix-style subdir with trailing sep",
                    ("foo", "foo/bar/"),            "bar/",             "bar\\"
                "2-deep windows-style subdir with trailing sep",
                    ("foo", "foo\\bar\\"),          "../foo\\bar\\",    "bar\\"
                "2-deep unix-style subdir of absolute path",
                    ("/foo/bar", "/foo/bar/baz"),   "baz",              "baz"
                "2-deep windows-style subdir of absolute path",
                    ("C:\\foo\\bar", "C:\\foo\\bar\\baz"), "../C:\\foo\\bar\\baz", "baz"

                "relativeTo is 1-deep subdir of path (unix-style dir sep)",
                    ("/foo/bar", "/foo"),           "..",               ".."
                "relativeTo is 1-deep subdir of path (windows-style dir sep)",
                    ("C:\\foo\\bar", "C:\\foo"),    "../C:\\foo",       ".."
                "relativeTo is 2-deep subdir of path (unix-style dir sep)",
                    ("/foo/bar/baz", "/foo"),       "../..",            "..\\.."
                "relativeTo is 2-deep subdir of path (windows-style dir sep)",
                    ("C:\\foo\\bar\\baz", "C:\\foo"),"../C:\\foo",      "..\\.."
                "relativeTo is 3-deep subdir of path (unix-style dir sep)",
                    ("/foo/bar/baz/qux", "/foo"),   "../../..",         "..\\..\\.."
                "relativeTo is 3-deep subdir of path (windows-style dir sep)",
                    ("C:\\foo\\bar\\baz\\qux", "C:\\foo"),"../C:\\foo", "..\\..\\.."
                
                "multi-level sibling paths (unix dir sep)",
                    ("foo/bar", "baz/qux"),         "../../baz/qux",    "..\\..\\baz\\qux"
                "multi-level sibling paths (windows dir sep)",
                    ("foo\\bar", "baz\\qux"),       "../baz\\qux",      "..\\..\\baz\\qux"
                "multi-level sibling paths (unix dir sep) with trailing dir sep",
                    ("foo/bar", "baz/qux/"),        "../../baz/qux/",   "..\\..\\baz\\qux\\"
                "multi-level sibling paths (windows dir sep) with trailing dir sep",
                    ("foo\\bar", "baz\\qux\\"),     "../baz\\qux\\",    "..\\..\\baz\\qux\\"
                
                "relativeTo is 2-deep subdir of path (unix-style dir sep) with trailing dir sep",
                    ("/foo/bar/baz/", "/foo/"),     "../..",            "..\\.."
                "relativeTo is 2-deep subdir of path (windows-style dir sep) with trailing dir sep",
                    ("C:\\foo\\bar\\baz\\", "C:\\foo\\"),"../C:\\foo\\","..\\.."

                "two absolute paths with nothing in common (windows-style dir sep)",
                    ("C:\\foo","D:\\bar"),          "../D:\\bar",       "D:\\bar"
            ]
            testList "IndependentTests" [
                for (caseName, (input1, input2), unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetRelativePath (input1, input2)
                            Expect.equal actual windowsExpected "Path.GetRelativePath Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetRelativePath (input1, input2)
                            Expect.equal actual unixExpected "Path.GetRelativePath Unix"
                        )
                    ]
            ]
            testList "OracleTests" [
#if !FABLE_COMPILER
                // these tests compare the output to the BCL implementation to verify that they match for a particular
                // platform
                for (caseName, (input1, input2), unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase osName (fun () ->
                            let actual = if isWindows then windowsExpected else unixExpected
                            let expected = global.System.IO.Path.GetRelativePath (input1, input2)
                            Expect.equal actual expected ("Path.GetRelativePath " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    testList "Path" [
        yield isPathRootedTests
        yield combineTests
        yield! directorySeparatorTests
        yield getRelativePathTests
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