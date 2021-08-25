namespace Fable.System.IOImpl
open System
open System.Collections.Generic

#if FABLE_COMPILER
module internal Extensions =
    // TODO: we can get rid of this when Fable 3.2.10 hits NuGet:
    // https://github.com/fable-compiler/Fable/releases/tag/3.2.10
    type Uri with
        static member TryCreate (uri, uriKind: UriKind) =
            try true, Uri (uri, uriKind)
            with e ->
                false, null
open Extensions
#endif

type IIOApi =
#if NETSTANDARD2_1
    abstract member AsyncReadAllLines: pathOrUri:string -> Async<string[]>
    abstract member AsyncReadAllText: pathOrUri:string -> Async<string>
#endif
    abstract member ReadAllLines: pathOrUri:string -> string[]
    abstract member ReadAllText: pathOrUri:string -> string
    abstract member ReadLines: pathOrUri:string -> IEnumerable<string>

[<AbstractClass>]
type IOApi() =
    let splitLines (text: string) =
        let r = System.Text.RegularExpressions.Regex("\r\n|\n")
        r.Split text

#if NETSTANDARD2_1
    abstract member AsyncReadAllText: pathOrUri:string -> Async<string>
#endif
    abstract member ReadAllText: pathOrUri:string -> string

    interface IIOApi with
#if NETSTANDARD2_1
        member this.AsyncReadAllLines pathOrUri : Async<string[]> = async {
            let! text = this.AsyncReadAllText pathOrUri
            return splitLines text
        }
        member this.AsyncReadAllText pathOrUri : Async<string> = this.AsyncReadAllText pathOrUri
#endif
        member this.ReadAllLines pathOrUri : string [] = 
            let text = this.ReadAllText pathOrUri
            splitLines text
        member this.ReadAllText pathOrUri : string = this.ReadAllText pathOrUri
        member this.ReadLines pathOrUri : IEnumerable<string> = upcast (this :> IIOApi).ReadAllLines pathOrUri

type file(fileApiOrCurrentPageGetter: Choice<IIOApi, (unit -> Uri)>, webApi: IIOApi) =
    let getIoForPath path =
        // RelativePath and AbsolutePath are ambiguous -- they could represent a URL *OR* a file path.
        // Thus, their behavior depends on whether we have a file api or not. If no file api, then we will later treat
        // it as a URL.
        // Url is straightforward -- something like http://website.com/blah
        // As well as LocalPath -- this can only either be a full windows-style path (like C:\\foo\\bar) or
        // a file URI, like file:\\foo\\bar
        // Note, that as a consequence, Unix-style naked file paths are ambiguous absolute paths
        let (|RelativePath|AbsolutePath|Url|LocalPath|Unknown|) (p: Uri, originalString) =
            // use Windows logic as to not break the JS test suite (see Fable.System.IO.fs)
            // TODO: Can't use Uri.OriginalString due to https://github.com/fable-compiler/Fable/issues/2520
            if Fable.Windows.System.IO.Path.IsPathRooted originalString then
                AbsolutePath ()
            elif p.IsAbsoluteUri then
                if p.Scheme = "file" then LocalPath ()
                else if p.Scheme = "http" || p.Scheme = "https" then Url ()
                else Unknown ()
            else
                RelativePath ()
        
        match Uri.TryCreate (path, UriKind.RelativeOrAbsolute) with
        | false, _ -> invalidArg (nameof(path)) "Path format could not be identified"
        | true, pathAsUri ->
            match fileApiOrCurrentPageGetter, (pathAsUri, path) with
            // .NET + local, relative, or absolute path = file system
            | Choice1Of2 fApi, (LocalPath | RelativePath | AbsolutePath) -> fApi, path
            // .NET or Fable + URL = HTTP
            | _, Url -> webApi, path
            // Fable + relative or absolute path = HTTP
            | Choice2Of2 getCurrentPage, (RelativePath | AbsolutePath) ->
                // Interpret the path relative to the current website/page.
                // For example, reading "/css/something.css" from "example.org/pages/something.html" will fetch
                // "example.org/css/something.css".
                let uri' = Uri(getCurrentPage (), path)
                webApi, uri'.AbsoluteUri
            | Choice1Of2 _, Unknown -> invalidArg (nameof(path)) "Path was not recognized as a local file path or URL"
            | Choice2Of2 _, (LocalPath | Unknown) -> invalidArg (nameof(path)) "Path was not recognized as a URL"

    new(fileApi: IIOApi, webApi) = file(Choice1Of2 fileApi, webApi)
    new(getCurrentPage: (unit -> Uri), webApi) = file(Choice2Of2 getCurrentPage, webApi)

#if NETSTANDARD2_1
    member this.AsyncReadAllLines path : Async<string[]> = async {
        let io, path' = getIoForPath path
        return! io.AsyncReadAllLines path'
    }
    member this.AsyncReadAllText path = async {
        let io, path' = getIoForPath path
        return! io.AsyncReadAllText path'
    }
#endif

    member this.ReadAllLines path : string[] =
        let io, path' = getIoForPath path
        io.ReadAllLines path'

    member this.ReadAllText path =
        let io, path' = getIoForPath path
        io.ReadAllText path'

    member this.ReadLines path : IEnumerable<string> =
        let io, path' = getIoForPath path
        // would be nice to have this actually lazy on Fable -- but XHR doesn't support streaming, so we'd have to use
        // something else
        io.ReadLines path'
