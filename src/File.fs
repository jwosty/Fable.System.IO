namespace Fable.System.IOImpl

type file(files: Map<string,string>) =
    member this.ReadAllText path =
        files.[path]

