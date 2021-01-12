namespace Fable.System

open System

module IO =
    type path(separator: string) =
        member this.Combine ([<ParamArray>] paths: string[]) =
            String.Join (separator, paths)

namespace Fable.Unix.System
module IO =
    let Path = Fable.System.IO.path("/")

namespace Fable.Windows.System
module IO =
    let Path = Fable.System.IO.path("\\")

