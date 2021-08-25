namespace Fable.System.IO.Tests
open System
#if FABLE_COMPILER
open Fable.Mocha
#else
open System.Runtime.InteropServices
open Expecto
#endif
open Utils
open Fable.Core

module EntryPoint =
    [<Tests>]
    let testSuite =
        testList "Fable" [
            Fable.System.IO.Path.tests
            Fable.System.IO.File.tests
        ]

#if FABLE_COMPILER
    [<Import("XMLHttpRequest", "xmlhttprequest")>]
    let XMLHttpRequest: obj = jsNative

    [<Emit("global.XMLHttpRequest = $0")>]
    let setGlobalXMLHttpRequest (xmlHttpRequest: obj) = jsNative
#endif

    [<EntryPoint>]
    let main args =
#if FABLE_COMPILER
        // Now, anything that uses XMLHttpRequest will work!
        setGlobalXMLHttpRequest XMLHttpRequest
#endif
        
        // Notice how we never actually reference Fable.System.IO.Path from the test suite, and are thus able to avoid
        // issue #5 (https://github.com/jwosty/Fable.System.IO/pull/7), as we don't need to test platform detection,
        // and the platform detection functionality from Fable.Extras crashes under Mocha.
        let result =
    #if FABLE_COMPILER
            Mocha.runTests testSuite
    
    #else
            runTestsWithArgs defaultConfig args testSuite
    #endif
        result
