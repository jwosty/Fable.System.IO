namespace Fable.System
//open Fable.SimpleHttp
#if FABLE_COMPILER
open Fable.Extras.Platform
#else
open System.Runtime.InteropServices
#endif

open Fable
open Fable.Core
open Fable.Core.JsInterop

open Browser
open Browser.Types

#if FABLE_COMPILER
type private WebApi() =
    inherit IOImpl.IOApi()
    let createXhr path useAsync =
        let xhr = XMLHttpRequest.Create()
        xhr.``open`` ("get", path, useAsync)
        xhr

#if NETSTANDARD2_1
    override this.AsyncReadAllText path =
        Async.FromContinuations (fun (cont, econt, ccont) ->
            let xhr = createXhr path true
            xhr.send ()
            xhr.onreadystatechange <- (fun _ ->
                if xhr.readyState = ReadyState.Done then
                    cont xhr.responseText
            )
        )
#endif
        
    override this.ReadAllText path =
        let xhr = createXhr path false
        xhr.send ()
        xhr.responseText

#else
type private FileApi() =
    interface IOImpl.IIOApi with
#if NETSTANDARD2_1
        member this.AsyncReadAllLines path = async {
            return! Async.AwaitTask (System.IO.File.ReadAllLinesAsync path)
        }
        member this.AsyncReadAllText path = async {
            return! Async.AwaitTask (System.IO.File.ReadAllTextAsync path)
        }
#endif
        member this.ReadAllLines path = System.IO.File.ReadAllLines path
        member this.ReadAllText path = System.IO.File.ReadAllText path
        member this.ReadLines path =  System.IO.File.ReadLines path

type private WebApi() =
    inherit IOImpl.IOApi()
    let makeWc () = new System.Net.WebClient()
    
#if NETSTANDARD2_1
    override this.AsyncReadAllText path = async {
        use wc = makeWc ()
        return! Async.AwaitTask (wc.DownloadStringTaskAsync path)
    }
#endif
    override this.ReadAllText path =
        use wc = makeWc ()
        wc.DownloadString path
#endif

[<Sealed; AbstractClass>]
type IO private() =
    static let mutable pathImpl = None

    // Use a lazily-initialized property, so that we don't unnecessarily trigger the platform detection in scenarios
    // where it can crash stuff (like under the Mocha test environment) (see PR #7)
    static member Path =
        match pathImpl with
        | Some p -> p
        | None ->
            let isWindows =
#if FABLE_COMPILER
                JSe.Platform.is.windows || JSe.Platform.is.uwp
#else
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
#endif
            let p =
                if isWindows then Fable.Windows.System.IO.Path
                else Fable.Unix.System.IO.Path
            pathImpl <- Some p
            p

    static member val File : Fable.System.IOImpl.file =
#if FABLE_COMPILER
        let getCurrentPage () = failwith "bang" //System.Uri Browser.Dom.document.location.href
        new Fable.System.IOImpl.file(getCurrentPage, WebApi())
#else
        new Fable.System.IOImpl.file(FileApi(), WebApi())
#endif
