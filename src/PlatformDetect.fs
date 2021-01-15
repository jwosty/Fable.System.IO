module PlatformDetect
open Fable
open Fable.Core
open Fable.Import

type IOS =
    abstract member windows: bool
    abstract member android: bool
    abstract member linux: bool
    abstract member linuxBased: bool
    abstract member macos: bool
    abstract member tizen: bool
    abstract member gui: bool
    abstract member node: bool
    abstract member web: bool
    abstract member worker: bool
    abstract member uwp: bool
    abstract member terminal: bool

[<ImportDefault("platform-detect/os.mjs")>]
let os : IOS = jsNative
