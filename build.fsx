#if FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

// F# 4.7 due to https://github.com/fsharp/FAKE/issues/2001
#r "paket:
nuget FSharp.Core 4.7.0
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket
nuget Fake.Tools.Git //"

#if !FAKE
#r "netstandard"
// #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System
open System.Text.RegularExpressions
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

Trace.log "line 30!"

let yarnCmd =
    lazy (
        Trace.log "GOT HERE yarnCmd"
        let result =
            match ProcessUtils.tryFindFileOnPath "yarn" with
            | Some yarn -> yarn
            | None -> failwith "cmd not found: yarn"
        Trace.logfn "DONE yarnCmd: %s" result
        result
        )

let project = Path.Combine ("src", "Fable.System.IO.fsproj")
let testProject = Path.Combine ("tests", "Fable.System.IO.Tests.fsproj")
let solution = Path.Combine (".", "Fable.System.IO.sln")

let artifactsDir = Path.Combine (".", "artifacts")

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

type PackageVersionInfo = { versionName: string; versionChanges: string }

let scrapeChangelog () =
    let changelog = System.IO.File.ReadAllText "CHANGELOG.md"
    Trace.logfn "changelog text: %O" changelog
    let regex = Regex("""## (?<Version>.*)\n+(?<Changes>(.|\n)*?)##""")
    let result = seq {
        for m in regex.Matches changelog ->
            {   versionName = m.Groups.["Version"].Value.Trim()
                versionChanges =
                    m.Groups.["Changes"].Value.Trim()
                        .Replace("    *", "    ◦")
                        .Replace("*", "•")
                        .Replace("    ", "\u00A0\u00A0\u00A0\u00A0") }
    }
    result

let changelog = scrapeChangelog () |> Seq.toList
Trace.logfn "changelog: %O"
let currentVersionInfo =
    List.tryHead changelog
    |> Option.defaultWith (fun () -> failwithf "Version info not found!")
Trace.logfn "currentVersionInfo: %O" currentVersionInfo

let addProperties props (defaults) =
    { defaults with MSBuild.CliArguments.Properties = [yield! defaults.Properties; yield! props]}

let addVersionInfo (versionInfo: PackageVersionInfo) options =
    let versionPrefix, versionSuffix =
        match String.splitStr "-" versionInfo.versionName with
        | [hd] -> hd, None
        | hd::tl -> hd, Some (String.Join ("-", tl))
        | [] -> failwith "Version name is missing"
    addProperties [
        "VersionPrefix", versionPrefix
        match versionSuffix with Some versionSuffix -> "VersionSuffix", versionSuffix | _ -> ()
        "PackageReleaseNotes", versionInfo.versionChanges
    ] options

let buildCfg = DotNet.BuildConfiguration.fromEnvironVarOrDefault "configuration" DotNet.Release
let defaultMsbuildParams msbuildParams =
    msbuildParams
    |> addVersionInfo currentVersionInfo
    |> addProperties ["RepositoryCommit", Git.Information.getCurrentSHA1 "."]

let mkDefaultBuildOptions (options: DotNet.BuildOptions) =
    { options with
        Configuration = buildCfg
        MSBuildParams = defaultMsbuildParams options.MSBuildParams
    }

let mkDefaultPackOptions (options: DotNet.PackOptions) =
    { options with
        Configuration = buildCfg
        MSBuildParams = defaultMsbuildParams options.MSBuildParams
    }

let mkDefaultTestOptions (options: DotNet.TestOptions) =
    { options with
        Configuration = buildCfg
        MSBuildParams = defaultMsbuildParams options.MSBuildParams
    }

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
    
    DotNet.exec id "clean" "" |> ignore
    File.deleteAll !!(Path.Combine("**","*.fs.js"))
    
    Shell.cleanDir artifactsDir
)

Target.create "Restore" (fun _ ->
    Trace.log " --- Restoring --- "
    DotNet.exec id "tool" "restore" |> Trace.logfn "%O"
    DotNet.exec id "paket" "restore" |> Trace.logfn "%O"
    DotNet.restore id |> Trace.logfn "%O"
    if Shell.Exec (yarnCmd.Value, "install") <> 0 then
        failwith "yarn install failed"
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building --- "
    project |> DotNet.build mkDefaultBuildOptions
)

Target.create "Test" (fun _ ->
    Trace.log " --- Running tests --- "
    DotNet.test mkDefaultTestOptions solution
    if Shell.Exec (yarnCmd.Value, "test") <> 0 then
        failwith "yarn test failed"
)

Target.create "Pack" (fun _ ->
    Trace.log " --- Packing NuGet packages --- "
    project
    |> DotNet.pack (
        mkDefaultPackOptions
        >> (fun options -> { options with OutputPath = Some artifactsDir })
    )
)

Target.create "TestAll" ignore

Target.create "All" ignore

open Fake.Core.TargetOperators

// *** Define Dependencies ***

"Restore"
    ==> "Build"
    ==> "Pack"
    ==> "All"

"Test"
    ==> "TestAll"

// *** Start Build ***
Target.runOrDefault "Build"