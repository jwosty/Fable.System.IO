namespace Fable.System

open System
open System.Text

module IO =
    let private (|AllEmptyStrings|_|) (xs: string list) =
        if xs |> List.forall String.IsNullOrEmpty then Some () else None

    type path(directorySeparatorChar: char, altDirectorySeparatorChar: char, usesDrives: bool) =        
        let allDirSeparators = Set.ofList [directorySeparatorChar; altDirectorySeparatorChar]
        let allDirSeparatorsArray = Set.toArray allDirSeparators

        let directorySeparatorString = string directorySeparatorChar

        member _.IsPathRooted (path: string) =
            if path.Length = 0 then false
            else if allDirSeparators.Contains path.[0] then true
            else if path.Length = 1 then false
            else if usesDrives then
                    let driveLetter = path.[0]
                    (path.[1] = ':') && (int driveLetter) >= (int 'A') && (int driveLetter) <= (int 'z')
            else false

        // TODO: implement GetInvalidPathChars() and throw whenever those chars are used        
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
                    if not (allDirSeparators.Contains lastChar) then
                        sb.Append directorySeparatorChar |> ignore
                    sb.Append p |> ignore
                    lastChar <- p.[p.Length - 1]
                sb.ToString ()

        member _.GetRelativePath (relativeTo: string, path: string) =
            let trimmedRelativeTo = relativeTo.TrimEnd allDirSeparatorsArray
            let trimmedPath = path.TrimEnd allDirSeparatorsArray
            if trimmedRelativeTo = trimmedPath then
                "."
            else
                let rec eatCommonParts (relativeToParts: string list) (pathParts: string list) =
                    match relativeToParts, pathParts with
                    | r::rs, p::ps when r = p -> eatCommonParts rs ps
                    | rs, ps -> rs, ps
                
                let differingRelativeTo, differingPath =
                    eatCommonParts  (Array.toList (relativeTo.Split allDirSeparatorsArray))
                                    (Array.toList (path.Split allDirSeparatorsArray))
                
                match differingRelativeTo, differingPath with
                | ([] | [""]), ps -> String.Join (directorySeparatorString, ps)
                | (_::_ as rs), ps ->
                    let parts = [|
                        for _ in 1..rs.Length -> ".."
                        yield! ps
                    |]
                    String.Join(directorySeparatorString, parts)



        member _.DirectorySeparatorChar : char = directorySeparatorChar
        member _.AltDirectorySeparatorChar : char = altDirectorySeparatorChar

namespace Fable.Unix.System
module IO =
    let Path = Fable.System.IO.path('/', '/', false)

namespace Fable.Windows.System
module IO =
    let Path = Fable.System.IO.path('\\', '/', true)
