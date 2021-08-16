namespace Fable.System

#if FABLE_COMPILER
open Fable.Extras.Platform
#else
open System.Runtime.InteropServices
#endif

type private FileApi() =
    interface IOImpl.IIOApi with
        member this.ReadAllText path = System.IO.File.ReadAllText path

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

    static member val File =
        new Fable.System.IOImpl.file(new FileApi())
