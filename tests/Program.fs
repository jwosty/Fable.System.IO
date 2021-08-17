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

    [<EntryPoint>]
    let main args =
        // Notice how we never actually reference Fable.System.IO.Path from the test suite, and are thus able to avoid
        // issue #5 (https://github.com/jwosty/Fable.System.IO/pull/7), as we don't need to test platform detection,
        // and the platform detection functionality from Fable.Extras crashes under Mocha.

    #if FABLE_COMPILER
        Mocha.runTests testSuite
    #else
        runTestsWithArgs defaultConfig args testSuite
    #endif