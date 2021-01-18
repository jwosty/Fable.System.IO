# Fable.System.IO

``Fable.System.IO`` is a no-dependency, F#-only implementation of certain parts of the ``System.IO`` API. At the moment, this library only implements most of the methods in ``System.IO.Path``. This library is built to be completely Fable-compatible, and behave exactly the same whether targetting .NET or Javascript.

To use the library, reference the Fable.System.IO nuget package. When targetting Fable, you also need the ``platform-detect`` NPM package, installed like so:

```Shell
npm install platform-detect@3.0.1
```

Or:

```Shell
yarn add platform-detect@3.0.1
```

Then, wherever you ``open System.IO``, make sure you ``open Fable`` before it. For example, you must do the following:

```F#
open Fable
open ...
open System.IO
```

The following will **NOT** work:

```F#
// Wrong order! This won't work!
open System.IO
open ...
open Fable
```

``Fable.System.IO`` shadows ``System.IO``, so that any time you use the Path APIs, for example, the ``open Fable`` statement causes the compiler to use ``Fable.System.IO``'s implementations instead of the BCL implementations.

Fable.System.IO will behave the same as the browser's current platform, as detected by [platform-detect](https://www.npmjs.com/package/platform-detect). In other words, when running in a Unix agent, it will use ``/`` as the directory separator; and in a Windows agent, it will use ``\`` instead.

## Choosing OS to emulate

By default, this library will emulate the path behavior for the current detected operating system. If you want to make it always behave like Windows, you can ``open Fable.Windows``. To always emulate Unix path behavior, ``open Fable.Unix``. For example:

```F#
open Fable
open System.IO
open Fable.Unix

printfn "Path.Combine(\"foo\", \"bar\") = \"%s\"" (Path.Combine ("foo", "bar"))

// This will output "foo/bar" on all systems
```

## Supported APIs

Here is a list of currently implemented APIs in Fable.System.IO:

* System.IO
    * Path ([click here for Microsoft docs](https://docs.microsoft.com/en-us/dotnet/api/system.io.path?view=net-5.0))
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

## Development

You can open the [Fable.System.IO.sln](Fable.System.IO.sln) directly in your favorite .NET IDE.

You can run all tests (.NET and Javascript) with:

```Shell
dotnet tool restore
dotnet paket restore
dotnet fable build -t Test
```

All the .NET tests include "Oracle" tests cases, which test the API against the BCL implementation. Therefore, for complete testing, one should make sure to run the test suite under a Windows system _and_ a Unix system. The GitHub CI runs the full test suite under Windows and Ubuntu.