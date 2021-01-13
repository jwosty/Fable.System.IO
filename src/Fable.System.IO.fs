namespace Fable.System

open System
open System.Text

module IO =
    type path(pathSeparator: string) =
        member this.IsPathRooted (path: string) =
            if path.Length = 0 then false
            else if path.[0] = '/' || path.[0] = '\\' then true
            else if path.Length = 1 then false
            else
                let driveLetter = path.[0]
                (path.[1] = ':') && (int driveLetter) >= (int 'A') && (int driveLetter) <= (int 'z')
        
        member this.Combine ([<ParamArray>] paths: string[]) =
            let paths = paths |> Array.filter (fun p -> p <> "")
            let skipUntilLastRooted paths =
                let skipI = paths |> Array.tryFindIndexBack this.IsPathRooted |> Option.defaultValue 0
                paths |> Array.skip skipI
            match skipUntilLastRooted paths with
            | [||] -> ""
            | [|p|] -> p
            | paths ->
                let sb = new StringBuilder()
                sb.Append (paths.[0]) |> ignore
                // can't use StringBuilder.Chars nor StringBuilder.Length because they're not implemented in Fable
                let mutable lastChar = paths.[0].[paths.[0].Length - 1]
                for p in Seq.tail paths do
                    if not (lastChar = '/' || lastChar = '\\') then
                        sb.Append pathSeparator |> ignore
                    sb.Append p |> ignore
                    lastChar <- p.[p.Length - 1]
                sb.ToString ()

namespace Fable.Unix.System
module IO =
    let Path = Fable.System.IO.path("/")

namespace Fable.Windows.System
module IO =
    let Path = Fable.System.IO.path("\\")

