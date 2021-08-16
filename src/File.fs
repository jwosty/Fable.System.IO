namespace Fable.System.IOImpl

type IIOApi =
    abstract member ReadAllText: string -> string

type file(fileApi: IIOApi) =
    member this.ReadAllText path = fileApi.ReadAllText path
