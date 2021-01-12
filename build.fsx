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

let yarnCmd =
    lazy (
        match ProcessUtils.tryFindFileOnPath "yarn" with
        | Some yarn -> yarn
        | None -> failwith "cmd not found: yarn")

let project = Path.Combine ("src", "Fable.System.IO.fsproj")
let testProject = Path.Combine ("tests", "Fable.System.IO.Tests.fsproj")
let solution = Path.Combine (".", "Fable.System.IO.sln")

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
    
    DotNet.exec id "clean" "" |> ignore
    File.deleteAll !!"**/*.fs.js"
)

Target.create "Restore" (fun _ ->
    Trace.log " --- Restoring --- "
    DotNet.exec id "tool" "restore" |> Trace.logfn "%O"
    DotNet.exec id "paket" "restore" |> Trace.logfn "%O"
    DotNet.restore id |> Trace.logfn "%O"
    Shell.Exec (yarnCmd.Force(), "install") |> ignore
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building --- "
    DotNet.build id solution
)

Target.create "Test" (fun _ ->
    Trace.log " --- Running tests --- "
    DotNet.test id solution
    Shell.Exec (yarnCmd.Force(), "test") |> ignore
)

Target.create "Pack" (fun _ ->
    Trace.log " --- Packing NuGet packages --- "
    raise (NotImplementedException())
)

Target.create "TestAll" ignore

Target.create "All" ignore

open Fake.Core.TargetOperators

// *** Define Dependencies ***

"Restore"
    ==> "Build"
    ==> "All"

"Test"
    ==> "TestAll"

// *** Start Build ***
Target.runOrDefault "Build"