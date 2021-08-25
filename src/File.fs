namespace Fable.System.IOImpl
open System

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
    abstract member AsyncReadAllText: string -> Async<string>
    abstract member ReadAllText: string -> string

type file(fileAprOrCurrentPageGetter: Choice<IIOApi, (unit -> Uri)>, webApi: IIOApi) =
    new(fileApi: IIOApi, webApi) = file(Choice1Of2 fileApi, webApi)
    new(getCurrentPage: (unit -> Uri), webApi) = file(Choice2Of2 getCurrentPage, webApi)

    member this.AsyncReadAllText path = async {
        return raise (NotImplementedException())
    }

    member this.ReadAllText path =
        let (|RelativePath|Url|LocalPath|Unknown|) (p: Uri) =
            if p.IsAbsoluteUri then
                if p.Scheme = "file" then LocalPath ()
                else if p.Scheme = "http" || p.Scheme = "https" then Url ()
                else Unknown ()
            else
                RelativePath ()
        
        match Uri.TryCreate (path, UriKind.RelativeOrAbsolute) with
        | false, _ -> invalidArg (nameof(path)) "Path format could not be identified"
        | true, pathAsUri ->
            match fileAprOrCurrentPageGetter, pathAsUri with
            | Choice1Of2 fApi, (LocalPath | RelativePath) -> fApi.ReadAllText path
            | _, Url -> webApi.ReadAllText path
            | Choice2Of2 getCurrentPage, RelativePath ->
                let uri' = Uri(getCurrentPage (), path)
                webApi.ReadAllText uri'.AbsoluteUri
            | Choice1Of2 _, Unknown -> invalidArg (nameof(path)) "Path was not recognized as a local file path or URL"
            | Choice2Of2 _, (LocalPath | Unknown) -> invalidArg (nameof(path)) "Path was not recognized as a local file path or URL"
