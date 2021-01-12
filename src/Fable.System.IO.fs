namespace Fable.System

open System

module IO =
    type path(separator: string) =
        member this.IsPathRooted (path: string) =
            if path.Length = 0 then false
            else if path.[0] = '/' || path.[0] = '\\' then true
            else if path.Length = 1 then false
            else
                let driveLetter = path.[0]
                (path.[1] = ':') && (int driveLetter) >= (int 'A') && (int driveLetter) <= (int 'z')
        
        member this.Combine ([<ParamArray>] paths: string[]) =
            String.Join (separator, paths)

namespace Fable.Unix.System
module IO =
    let Path = Fable.System.IO.path("/")

namespace Fable.Windows.System
module IO =
    let Path = Fable.System.IO.path("\\")

