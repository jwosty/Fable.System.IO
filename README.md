# Fable.System.IO [![Nuget](https://img.shields.io/nuget/v/Fable.System.IO.svg?maxAge=0&colorB=brightgreen&label=Fable.System.IO)](https://www.nuget.org/packages/Fable.System.IO) ![CI (Windows)](https://github.com/jwosty/Fable.System.IO/workflows/CI%20(Windows)/badge.svg) ![CI (Ubuntu)](https://github.com/jwosty/Fable.System.IO/workflows/CI%20(Ubuntu)/badge.svg)

``Fable.System.IO`` is a Fable-compatible implementation of Path and some of the File APIs from .NET. This library is built to behave identically to the real .NET APIs, whether using it from .NET or Javascript.

To use the library, just reference the [Fable.System.IO NuGet package](https://www.nuget.org/packages/Fable.System.IO/) and replace all occurrences of ``open System.IO`` with ``open Fable.System.IO``.

Fable.System.IO will behave the same as the browser's current platform, as detected by [platform-detect](https://www.npmjs.com/package/platform-detect). In other words, when running in a Unix agent, it will use ``/`` as the directory separator; and in a Windows agent, it will use ``\`` instead.

## File APIs in Fable?!

Fable.System.IO gives File read operations the ability to make web requests, and will most of the time "do the right thing" when used in a Fable context. For example (works in .NET and Fable):

```fsharp
open Fable.System.IO // Don't forget this!
// Get the index page of Google.
let google = File.ReadAllText "https://google.com"
```

Or, if we're on example.org/foo/bar.html, and you want to get a file right beside the html page called data.csv:

```fsharp
// If running in Fable, and browser is at example.org/foo/bar.html, this fetches example.org/foo/data.csv
// If running on .NET, this will behave as usual (read the file from the current working directory)
let data = File.ReadAllLines "./data.csv"
```

Or, if we're on example.org/foo/bar.html and you want to read example.org/some/thing/else.json, asynchronously:

```fsharp
async {
    let! data = File.AsyncReadAllLines "../some/thing/else.json"
    // alternatively
    let! data = File.AsyncReadAllLines "/some/thing/else.json"
}
```

## Choosing emulated OS

By default, this library will emulate the path behavior for the current detected operating system. If you want to force Windows path behavior, you can ``open Fable.Windows.System.IO``. Likewise, to force Unix path behavior, ``open Fable.Unix.System.IO``. For example:

```F#
open Fable
open Fable.Unix.System.IO

printfn "Path.Combine(\"foo\", \"bar\") = \"%s\"" (Path.Combine ("foo", "bar"))

// This will output "foo/bar" on all systems
```

## Supported APIs

Here is a list of currently implemented APIs in Fable.System.IO:

* System.IO
    * Path (static methods) ([Microsoft docs here](https://docs.microsoft.com/en-us/dotnet/api/system.io.path?view=net-5.0))
        * ``GetInvalidFileNameChars()``
        * ``GetInvalidPathChars()``
        * ``IsPathRooted(string)``
        * ``Combine(string[])``
        * ``Join(string, string)``
        * ``GetRelativePath(string, string)``
        * ``GetDirectoryName(string)``
        * ``GetFileName(string)``
        * ``GetFileNameWithoutExtension(string)``
        * ``GetExtension(string)``
        * ``HasExtension(string)``
        * ``DirectorySeparatorChar : string``
        * ``AltDirectorySeparatorChar : string``
    * File (static methods) ([Microsoft docs here](https://docs.microsoft.com/en-us/dotnet/api/system.io.file?view=net-5.0))
        * File.ReadAllLines
        * *File.AsyncReadAllLines<sup>†</sup>*
        * File.ReadAllText
        * *File.AsyncReadAllText<sup>†</sup>*
        * File.ReadLines

† Convenience methods (not part of the BCL) that can be used in place of the asynchronous Task<'T> variations. You can safely use these instead of the BCL async methods on both .NET and Fable runtimes.

## Development

You can open the [Fable.System.IO.sln](Fable.System.IO.sln) directly in your favorite .NET IDE.

You can run all tests (.NET and Javascript) with:

```Shell
dotnet tool restore
dotnet paket restore
dotnet fable build -t Test
```

To run the test suite only under .NET, use:

```Shell
dotnet test
```

To run the test suite only under node, use:

```Shell
yarn test
```


The Path tests include "Oracle" tests cases, which test the API against the BCL implementation. Therefore, for complete testing, one should make sure to run the test suite under a Windows system _and_ a Unix system. The GitHub CI runs the full test suite under Windows and Ubuntu.
