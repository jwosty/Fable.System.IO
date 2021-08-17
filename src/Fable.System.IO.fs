namespace Fable.System
open Fable.SimpleHttp
#if FABLE_COMPILER
open Fable.Extras.Platform
#else
open System.Runtime.InteropServices
#endif

open Fable
open Fable.Core
open Fable.Core.JsInterop

#if FABLE_COMPILER
type private WebApi() =

    interface IOImpl.IIOApi with
        member this.ReadAllText path =
            let mutable result = None
            Async.StartImmediate <| async {
                printfn "GETTING PATH: %A" path
                let! resp = Http.get path
                printfn "GOT RESPONSE: %A" resp
                match resp with
                | 200, response ->
                    result <- Some response
                | statusCode, _ -> failwithf "The server returned an error: %d" statusCode
            }
            while (result = None) do ()
            result.Value

#else
type private FileApi() =
    interface IOImpl.IIOApi with
        member this.ReadAllText path = System.IO.File.ReadAllText path
type private WebApi() =
    let webClient = new System.Net.WebClient()
    let _webClientLock = obj()

    interface IOImpl.IIOApi with
        member this.ReadAllText path =
            // WebClient does not allow concurrent access
            lock _webClientLock (fun () ->
                webClient.DownloadString path
            )
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
        let getCurrentPage () = System.Uri Browser.Dom.document.location.href
        new Fable.System.IOImpl.file(getCurrentPage, WebApi())
#else
        new Fable.System.IOImpl.file(FileApi(), WebApi())
#endif
