namespace Fable.System

open System
open System.Text

module IO =
    let private (|AllEmptyStrings|_|) (xs: string list) =
        if xs |> List.forall String.IsNullOrEmpty then Some () else None

    type path internal(directorySeparatorChar: char, altDirectorySeparatorChar: char, usesDrives: bool,
                       getInvalidFilenameChars: unit -> char[], getInvalidPathChars: unit -> char[]) =        
        let allDirSeparators = Set.ofList [directorySeparatorChar; altDirectorySeparatorChar]
        let allDirSeparatorsArray = Set.toArray allDirSeparators

        let directorySeparatorString = string directorySeparatorChar

        member _.GetInvalidFileNameChars () = getInvalidFilenameChars ()
        member _.GetInvalidPathChars () = getInvalidPathChars ()

        member _.IsPathRooted (path: string) =
            if path.Length = 0 then false
            else if allDirSeparators.Contains path.[0] then true
            else if path.Length = 1 then false
            else if usesDrives then
                    let driveLetter = path.[0]
                    (path.[1] = ':') && (int driveLetter) >= (int 'A') && (int driveLetter) <= (int 'z')
            else false

        member this.Combine ([<ParamArray>] paths: string[]) =
            if isNull paths then
                raise (ArgumentNullException(nameof(paths)))
            
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

        member this.Join ([<ParamArray>] paths: string[]) =
            let sb = StringBuilder()
            let mutable lastPathEndsInDirSep = false

            let inline beginsInDirSep (path: string) = path.Length > 0 && allDirSeparators.Contains (path.[0])

            for i in 0 .. paths.Length - 1 do
                let path = paths.[i]
                if i > 0 && not lastPathEndsInDirSep && not (beginsInDirSep path) then
                    sb.Append directorySeparatorChar |> ignore
                sb.Append path |> ignore
                lastPathEndsInDirSep <- path.Length > 0 && allDirSeparators.Contains (path.[path.Length - 1])

            sb.ToString()

        member this.GetRelativePath (relativeTo: string, path: string) =
            if isNull relativeTo then
                raise (ArgumentNullException(nameof(relativeTo)))
            if isNull path then
                raise (ArgumentNullException(nameof(path)))
            
            let trimmedRelativeTo = relativeTo.TrimEnd allDirSeparatorsArray
            let trimmedPath = path.TrimEnd allDirSeparatorsArray
            if trimmedRelativeTo = trimmedPath then
                "."
            else
                let rec eatCommonParts hasCommonParts (relativeToParts: string list) (pathParts: string list) =
                    match relativeToParts, pathParts with
                    | r::rs, p::ps when r = p -> eatCommonParts true rs ps
                    | rs, ps -> hasCommonParts, rs, ps
                
                let hasCommonParts, differingRelativeTo, differingPath =
                    eatCommonParts false
                        (Array.toList (relativeTo.Split allDirSeparatorsArray))
                        (Array.toList (path.Split allDirSeparatorsArray))
                
                if not hasCommonParts && this.IsPathRooted relativeTo && this.IsPathRooted path then
                    // if they share nothing, meaning they have different roots, return ``path`` as is (i.e. C:\\foo
                    // and D:\\bar)
                    path
                else
                    let nonEmptyRs = differingRelativeTo |> Seq.filter (not << String.IsNullOrEmpty)
                    let parts = [|
                        for _ in 1 .. (Seq.length nonEmptyRs) -> ".."
                        // if differingPath is empty, that means relativeTo is a strict subdir of path, and we don't want
                        // to add any trailing slashes in that situation
                        if differingPath <> [""] then
                            yield! differingPath
                    |]
                    String.Join(directorySeparatorString, parts)

        member _.DirectorySeparatorChar : char = directorySeparatorChar
        member _.AltDirectorySeparatorChar : char = altDirectorySeparatorChar

namespace Fable.Unix.System
module IO =
    let private getInvalidFileNameChars () = [|'\000'; '/'|]
    let private getInvalidPathChars () = [|'\000'|]
    let Path = Fable.System.IO.path('/', '/', false, getInvalidFileNameChars, getInvalidPathChars)

namespace Fable.Windows.System
module IO =
    let private getInvalidFileNameChars () =
        [|'"'; '<'; '>'; '|'; '\000'; '\001'; '\002'; '\003'; '\004'; '\005'; '\006';
          '\007'; '\b'; '\009'; '\010'; '\011'; '\012'; '\013'; '\014'; '\015'; '\016';
          '\017'; '\018'; '\019'; '\020'; '\021'; '\022'; '\023'; '\024'; '\025'; '\026';
          '\027'; '\028'; '\029'; '\030'; '\031'; ':'; '*'; '?'; '\\'; '/'|]
    let private getInvalidPathChars () =
        [|'|'; '\000'; '\001'; '\002'; '\003'; '\004'; '\005'; '\006'; '\007'; '\b';
          '\009'; '\010'; '\011'; '\012'; '\013'; '\014'; '\015'; '\016'; '\017'; '\018';
          '\019'; '\020'; '\021'; '\022'; '\023'; '\024'; '\025'; '\026'; '\027'; '\028';
          '\029'; '\030'; '\031'|]
    let Path = Fable.System.IO.path('\\', '/', true, getInvalidFileNameChars, getInvalidPathChars)
