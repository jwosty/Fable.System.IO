namespace Fable.Mocha
open Fable.Mocha

module Expect =
    let inline sequenceEqual actual expected message =
        let actualList = Seq.toList actual
        let expectedList = Seq.toList expected
        Expect.equal actualList expectedList message