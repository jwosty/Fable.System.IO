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
    let invalidFilenameAndPathChars = [
        testList "InvalidFileNameChars" [
#if !FABLE_COMPILER
            testList "OracleTests" [
                testCase osName (fun () ->
                    let actual =
                        if isWindows then
                            Fable.Windows.System.IO.Path.GetInvalidFileNameChars ()
                        else
                            Fable.Unix.System.IO.Path.GetInvalidFileNameChars ()
                    Expect.equal actual (global.System.IO.Path.GetInvalidFileNameChars ())
                        ("Path.GetInvalidFileNameChars " + osName + " (Oracle)")
                )
            ]
#endif
        ]
        testList "InvalidPathChars" [
#if !FABLE_COMPILER
            testList "OracleTests" [
                testCase osName (fun () ->
                    let actual =
                        if isWindows then
                            Fable.Windows.System.IO.Path.GetInvalidPathChars ()
                        else
                            Fable.Unix.System.IO.Path.GetInvalidPathChars ()
                    Expect.equal actual (global.System.IO.Path.GetInvalidPathChars ())
                        ("Path.GetInvalidPathChars " + osName + " (Oracle)")
                )
            ]
#endif
        ]
    ]
    
    let getPathRootTests =
        let testCases = [
            "empty path",
                "",     null,   null
            "whitespace-only path",
                "    ", "",     null
            "simple unix-style relative path", "foo/bar", "", ""
            "simple windows-style relative path", "foo\\bar", "", ""
            "simple mixed-style relative path", "foo/bar\\baz", "", ""

            "explicit current dir",
                ".", "", ""
            "explicit parent dir",
                "..", "", ""
            "explicit current dir with trailing unix path separator",
                "./", "", ""
            "explicit current dir with trailing windows path separator",
                ".\\", "", ""
            "explicit parent dir with trailing unix path separator",
                "../", "", ""
            "explicit parent dir with trailing windows path separator",
                "..\\", "", ""

            "unix root",
                "/",        "/",    "\\"
            "simple unix absolute path",
                "/foo",     "/",    "\\"
            "simple unix absolute path with trailing separators",
                "/foo/",    "/",    "\\"
            // TODO: implement UNC paths
            //"short UNC path with unix-style separators",
            //    "//foo",    "//",   "\\\\foo"
            //"longer UNC path with unix-style separators",
            //    "//foo/bar","//",   "\\\\foo\\bar"
            //"another UNC path with unix-style separators",
            //    "//foo/",   "//foo/","\\\\foo\\"
            "windows C drive 1 no slash",
                "C:",       "",     "C:"
            "windows C drive 2 slash",
                "C:\\",     "",     "C:\\"
            "windows C drive subfolder",
                "C:\\foo",  "",     "C:\\"
            "windows C drive forward slash",
                "C:/",      "",     "C:\\"
            "windows C drive subfolder no slash",
                "C:foo",    "",     "C:"
            "windows style root no drive letter",
                "\\",       "",     "\\"
            "windows style root subfolder no drive letter",
                "\\foo",    "",     "\\"
            "windows D drive forward slash",
                "D://abc",  "",     "D:\\"
            "windows z drive slash",
                "z:\\abc",  "",     "z:\\"
            "windows Z drive slash",
                "Z:\\abc",  "",     "Z:\\"
            "windows a drive slash",
                "a:\\abc",  "",     "a:\\"
            "windows A drive slash",
                "A:\\abc",  "",     "A:\\"

            "UNC named pipe",
                "\\.\\pipe\\MyPipe", "", "\\"
            
            "strange path", "::", "", ""
        ]
        testList "GetPathRoot" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetPathRoot input
                            Expect.equal actual windowsExpected "Path.GetPathRoot Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetPathRoot input
                            Expect.equal actual unixExpected "Path.GetPathRoot Unix"
                        )
                    ]
            ]
            testList "OracleTests" [
#if !FABLE_COMPILER
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                            testCase "Windows" (fun () ->
                                let oracle = global.System.IO.Path.GetPathRoot input
                                Expect.equal windowsExpected oracle "Path.GetPathRoot Windows (Oracle)"
                            )
                        else
                            testCase "Unix" (fun () ->
                                let oracle = global.System.IO.Path.GetPathRoot input
                                Expect.equal unixExpected oracle "Path.GetPathRoot Unix (Oracle)"
                            )
                    ]
#endif
            ]
        ]

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

    let joinTests =
        let testCases = [
            "1 empty part",
                [|""|],                     "",                 ""
            "1 part no dir sep",
                [|"foo"|],                  "foo",              "foo"
            "2 parts no dir sep",
                [|"foo";"bar"|],            "foo/bar",          "foo\\bar"
            "3 parts no dir sep",
                [|"foo";"bar";"baz"|],      "foo/bar/baz",      "foo\\bar\\baz"
            "3 parts some with windows dir sep some without",
                [|"foo";"bar\\";"baz"|],    "foo/bar\\/baz",    "foo\\bar\\baz"
            "3 parts some with unix dir sep some without",
                [|"foo";"bar/";"baz"|],     "foo/bar/baz",      "foo\\bar/baz"
            "3 parts with some other windows dir sep some without",
                [|"foo\\";"bar";"baz\\"|],  "foo\\/bar/baz\\",  "foo\\bar\\baz\\"
            "3 parts with some other unix dir sep some without",
                [|"foo/";"bar";"baz/"|],    "foo/bar/baz/",     "foo/bar\\baz/"
            "4 parts, some rooted, some not (windows-style)",
                [|"C:\\foo";"bar";"D:\\baz";"qux"|], "C:\\foo/bar/D:\\baz/qux", "C:\\foo\\bar\\D:\\baz\\qux"
            "4 parts, some rooted, some not (unix-style)",
                [|"/foo";"bar";"/baz";"qux"|],       "/foo/bar/baz/qux",        "/foo\\bar/baz\\qux"
        ]
        testList "Join" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.Join input
                            Expect.equal actual windowsExpected "Path.Join Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.Join input
                            Expect.equal actual unixExpected "Path.Join Unix"
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
                            let expected = global.System.IO.Path.Join input
                            Expect.equal actual expected ("Path.Join " + osName + " (Oracle)")
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

    let getDirectoryNameTests =
        let testCases = [
            "empty string",
                "",                 (null:string),  (null:string)
            "relative path - 1 part no sep",
                "foo",              "",             ""
            "relative path - 1 part trailing sep (unix sep)",
                "foo/",             "foo",          "foo"
            "relative path - 1 part trailing sep (windows sep)",
                "foo\\",            "",             "foo"
            "relative path - 2 part trailing sep (unix sep)",
                "foo/bar/",         "foo/bar",      "foo\\bar"
            "relative path - 2 part trailing sep (windows sep)",
                "foo\\bar\\",       "",             "foo\\bar"
            "relative path - 2 part no trailing sep (unix sep)",
                "foo/bar",          "foo",          "foo"
            "relative path - 2 part no trailing sep (windows sep)",
                "foo\\bar",         "",             "foo"
            "absolute path - 1 part no trailing sep (windows style)",
                "C:\\foo",          "",             "C:\\"
            "absolute path - 1 part no trailing sep (unix style)",
                "/foo",             "/",            "\\"
            "absolute path - 2 part no trailing sep nor sep after drive (windows style)",
                "C:foo",            "",             "C:"
            "root path (windows style)",
                "C:\\",             "",             null
            "root path (unix style)",
                "/",                null,           null
            "explicit relative path - 1 part trailing sep (windows style)",
                ".\\",              "",             "."
            "explicit relative path - 1 part trailing sep (unix style)",
                "./",               ".",            "."
            "explicit relative path - 1 part no trailing sep",
                ".",                "",             ""
            "just whitespaces",
                "    ",             "",             null
            "null path",
                null,               null,           null
        ]
        testList "GetDirectoryName" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetDirectoryName input
                            Expect.equal actual windowsExpected "Path.GetDirectoryName Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetDirectoryName input
                            Expect.equal actual unixExpected "Path.GetDirectoryName Unix"
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
                            let expected = global.System.IO.Path.GetDirectoryName input
                            Expect.equal actual expected ("Path.GetDirectoryName " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    let getFileNameTests =
        let testCases = [
            "empty string",
                "",                 "",                 ""
            "relative path - 1 part without file extension",
                "foo",              "foo",              "foo"
            "relative path - 1 part with file extension",
                "foo.txt",          "foo.txt",          "foo.txt"
            "relative path - 3 part with file extension (windows dir sep)",
                "foo\\bar\\baz.txt","foo\\bar\\baz.txt","baz.txt"
            "relative path - 3 part with file extension (unix dir sep)",
                "foo/bar/baz.txt",  "baz.txt",          "baz.txt"
            "relative path - 3 part with no file extension (windows dir sep)",
                "foo\\bar\\baz",    "foo\\bar\\baz",    "baz"
            "relative path - 3 part with no file extension (unix dir sep)",
                "foo/bar/baz",      "baz",              "baz"
            "relative path - 3 part with trailing sep (windows dir sep)",
                "foo\\bar\\baz\\",  "foo\\bar\\baz\\",  ""
            "relative path - 3 part with trailing sep (unix dir sep)",
                "foo/bar/baz/",     "",                 ""
        ]
        testList "GetFileName" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetFileName input
                            Expect.equal actual windowsExpected "Path.GetFileName Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetFileName input
                            Expect.equal actual unixExpected "Path.GetFileName Unix"
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
                            let expected = global.System.IO.Path.GetFileName input
                            Expect.equal actual expected ("Path.GetDirectoryName " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    let getFileNameWithoutExtensionTests =
        let testCases = [
            "empty string",
                "",                 "",                 ""
            "relative path - 1 part without file extension",
                "foo",              "foo",              "foo"
            "relative path - 1 part with file extension",
                "foo.txt",          "foo",              "foo"
            "relative path - 2 part with 2 part file extension (windows dir sep)",
                "foo\\bar.baz.txt", "foo\\bar.baz",     "bar.baz"
            "relative path - 2 part with 2 part file extension (unix dir sep)",
                "foo/bar.baz.txt",  "bar.baz",          "bar.baz"
            "relative path - 3 part with file extension (windows dir sep)",
                "foo\\bar\\baz.txt","foo\\bar\\baz",    "baz"
            "relative path - 3 part with file extension (unix dir sep)",
                "foo/bar/baz.txt",  "baz",              "baz"
            "relative path - 3 part with no file extension (windows dir sep)",
                "foo\\bar\\baz",    "foo\\bar\\baz",    "baz"
            "relative path - 3 part with no file extension (unix dir sep)",
                "foo/bar/baz",      "baz",              "baz"
            "relative path - 3 part with trailing sep (windows dir sep)",
                "foo\\bar\\baz\\",  "foo\\bar\\baz\\",  ""
            "relative path - 3 part with trailing sep (unix dir sep)",
                "foo/bar/baz/",     "",                 ""
        ]
        testList "GetFileNameWithoutExtension" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetFileNameWithoutExtension input
                            Expect.equal actual windowsExpected "Path.GetFileNameWithoutExtension Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetFileNameWithoutExtension input
                            Expect.equal actual unixExpected "Path.GetFileNameWithoutExtension Unix"
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
                            let expected = global.System.IO.Path.GetFileNameWithoutExtension input
                            Expect.equal actual expected ("Path.GetFileNameWithoutExtension " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    let getExtension =
        let testCases = [
            "empty string",
                "",                 "",                 ""
            "relative path - 1 part without file extension",
                "foo",              "",                 ""
            "relative path - 1 part with file extension",
                "foo.txt",          ".txt",             ".txt"
            "relative path - 2 part with 2 part file extension (windows dir sep)",
                "foo\\bar.baz.txt", ".txt",             ".txt"
            "relative path - 2 part with 2 part file extension (unix dir sep)",
                "foo/bar.baz.txt",  ".txt",             ".txt"
            "relative path - 3 part with file extension (windows dir sep)",
                "foo\\bar\\baz.txt",".txt",             ".txt"
            "relative path - 3 part with file extension (unix dir sep)",
                "foo/bar/baz.txt",  ".txt",             ".txt"
            "relative path - 3 part with no file extension (windows dir sep)",
                "foo\\bar\\baz",    "",                 ""
            "relative path - 3 part with no file extension (unix dir sep)",
                "foo/bar/baz",      "",                 ""
            "relative path - 3 part with trailing sep (windows dir sep)",
                "foo\\bar\\baz\\",  "",                 ""
            "relative path - 3 part with trailing sep (unix dir sep)",
                "foo/bar/baz/",     "",                 ""
        ]
        testList "GetExtension" [
            testList "IndependentTests" [
                for (caseName, input, unixExpected, windowsExpected) in testCases ->
                    testList caseName [
                        testCase "Windows" (fun () ->
                            let actual = Fable.Windows.System.IO.Path.GetExtension input
                            Expect.equal actual windowsExpected "Path.GetExtension Windows"
                        )
                        testCase "Unix" (fun () ->
                            let actual = Fable.Unix.System.IO.Path.GetExtension input
                            Expect.equal actual unixExpected "Path.GetExtension Unix"
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
                            let expected = global.System.IO.Path.GetExtension input
                            Expect.equal actual expected ("Path.GetExtension " + osName + " (Oracle)")
                        )
                    ]
#endif
            ]
        ]

    testList "Path" [
        yield! invalidFilenameAndPathChars
        yield getPathRootTests
        yield isPathRootedTests
        yield combineTests
        yield joinTests
        yield! directorySeparatorTests
        yield getRelativePathTests
        yield getDirectoryNameTests
        yield getFileNameTests
        yield getFileNameWithoutExtensionTests
        yield getExtension
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