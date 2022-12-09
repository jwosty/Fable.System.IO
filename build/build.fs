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
        let result =
            match ProcessUtils.tryFindFileOnPath "yarnpkg" with
            | Some yarn -> yarn
            | None -> failwith "cmd not found: yarn"
        Trace.logfn "Executed yarnCmd: %s" result
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
    let changelog = File.ReadAllText "CHANGELOG.md"
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

let Clean _ =
    Trace.log " --- Cleaning --- "
    
    DotNet.exec id "clean" "" |> ignore
    File.deleteAll !!(Path.Combine("**","*.fs.js"))
    
    Shell.cleanDir artifactsDir

let Restore _ =
    Trace.log " --- Restoring --- "
    DotNet.exec id "tool" "restore" |> Trace.logfn "%O"
    DotNet.exec id "paket" "restore" |> Trace.logfn "%O"
    DotNet.restore id |> Trace.logfn "%O"
    if Shell.Exec (yarnCmd.Value, "install") <> 0 then
        failwith "yarn install failed"

let Build _ =
    Trace.log " --- Building --- "
    project |> DotNet.build mkDefaultBuildOptions

let Test _ =
    Trace.log " --- Running tests --- "
    DotNet.test mkDefaultTestOptions solution
    if Shell.Exec (yarnCmd.Value, "test") <> 0 then
        failwith "yarn test failed"

let Pack _ =
    Trace.log " --- Packing NuGet packages --- "
    project
    |> DotNet.pack (
        mkDefaultPackOptions
        >> (fun options -> { options with OutputPath = Some artifactsDir })
    )



open Fake.Core.TargetOperators

// *** Define Dependencies ***

let initTargets () =
    Target.create "Clean" Clean
    Target.create "Restore" Restore
    Target.create "Build" Build
    Target.create "Test" Test
    Target.create "Pack" Pack
    Target.create "TestAll" ignore
    Target.create "All" ignore
    
    "Restore"
        ==> "Build"
        ==> "Pack"
        ==> "All"
        |> ignore

    "Test"
        ==> "TestAll"
        |> ignore
    ()

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    initTargets ()
    Target.runOrDefaultWithArguments "Build"

    0

