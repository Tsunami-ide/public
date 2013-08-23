// Don't run this - it is for IntelliPrompt

// If you get red squigglies below, you probably don't have MBrace installed.
// To install it you will need to enroll for the Alpha version here:
//     http://www.m-brace.net/alpha-testing/

#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Base.dll"
#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Client.dll"
#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Store.dll"
#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Core.dll"
#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Actors.dll"
#r @"C:\Program Files (x86)\MBrace\bin\Nessos.MBrace.Utils.dll"
open Nessos
open Nessos.MBrace.Client

// Run this in mbi.exe

let runtime = MBrace.InitLocal 4

[<Cloud>]
let helloWorld () =
    cloud {
    return "Hello world"
}

runtime.Run <@ helloWorld () @>





