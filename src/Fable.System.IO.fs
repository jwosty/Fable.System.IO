namespace Fable.System.IOImpl

open System
open System.Text

type path internal(directorySeparatorChar: char, altDirectorySeparatorChar: char, usesDrives: bool,
                    getInvalidFilenameChars: unit -> char[], getInvalidPathChars: unit -> char[],
                    isPathEffectivelyEmpty: string -> bool) =        
    let allDirSeparators = Set.ofList [directorySeparatorChar; altDirectorySeparatorChar]
    let allDirSeparatorsArray = Set.toArray allDirSeparators

    let directorySeparatorString = string directorySeparatorChar

    let getRootLength (path: string) =
        if path.Length = 0 then 0
        else if allDirSeparators.Contains path.[0] then 1
        else if path.Length = 1 then 0
        else if usesDrives then
            let driveLetter = path.[0]
            if (path.[1] = ':') && (int driveLetter) >= (int 'A') && (int driveLetter) <= (int 'z') then
                3
            else 0
        else 0

    let normalizeDirSeparators (path: string) =
        path
        |> Seq.map (fun c -> if allDirSeparators.Contains c then directorySeparatorChar else c)
        |> Seq.toArray
        |> String

    let tryFindLastDirSepI (path: string) = path |> Seq.tryFindIndexBack (fun c -> allDirSeparators.Contains c)

    member _.GetInvalidFileNameChars () = getInvalidFilenameChars ()
    member _.GetInvalidPathChars () = getInvalidPathChars ()

    member _.IsPathRooted (path: string) = getRootLength path > 0

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

    member _.Join ([<ParamArray>] paths: string[]) =
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

    member _.GetDirectoryName (path: string) =
        if isPathEffectivelyEmpty path then
            null
        else
            let rootLength = getRootLength path

            if rootLength = path.Length then
                null
            else
                match tryFindLastDirSepI path with
                | Some lastDirSepI ->
                    // we take the max between rootLen and lastDirSepI because we want to stop it from going backwards
                    // past into the root (i.e. C:\foo should become C:\ and not C: )
                    path.[0 .. max (rootLength - 1) (lastDirSepI - 1)]
                    |> normalizeDirSeparators
                | None -> ""

    member private _.GetFileNameStartI path =
        tryFindLastDirSepI path
        |> Option.map ((+) 1)
        |> Option.defaultValue 0

    member private _.GetExtenionStartI (path: string) =
        path
        |> Seq.tryFindIndexBack ((=) '.')
        |> Option.defaultValue path.Length

    member this.GetFileName (path: string) =
        let startI = this.GetFileNameStartI path
        path.[startI .. path.Length - 1]

    member this.GetFileNameWithoutExtension (path: string) =
        let startI = this.GetFileNameStartI path
        let endI = this.GetExtenionStartI path - 1
        path.[startI .. endI]

    member this.GetExtension (path: string) =
        let startI = this.GetExtenionStartI path
        path.[startI .. path.Length - 1]

    member _.DirectorySeparatorChar : char = directorySeparatorChar
    member _.AltDirectorySeparatorChar : char = altDirectorySeparatorChar

namespace Fable.Unix.System
module IO =
    let private getInvalidFileNameChars () = [|'\000'; '/'|]
    let private getInvalidPathChars () = [|'\000'|]
    let Path =
        Fable.System.IOImpl.path(
            '/', '/', false, getInvalidFileNameChars, getInvalidPathChars,
            // see https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Private.CoreLib/src/System/IO/PathInternal.Unix.cs#L88
            System.String.IsNullOrEmpty)

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
    let Path =
        Fable.System.IOImpl.path(
            '\\', '/', true, getInvalidFileNameChars, getInvalidPathChars,
            // see https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Private.CoreLib/src/System/IO/PathInternal.Windows.cs#L401
            System.String.IsNullOrWhiteSpace)

namespace Fable.System

module IO =
#if !FABLE_COMPILER
    open System.Runtime.InteropServices
#endif
    let Path =
        let isWindows =
#if FABLE_COMPILER
            PlatformDetect.os.windows || PlatformDetect.os.uwp
#else
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
#endif
        if isWindows then Fable.Windows.System.IO.Path
        else Fable.Unix.System.IO.Path