#r @"C:\Program Files (x86)\FSharpPowerPack-4.0.0.0\bin\FSharp.Compiler.CodeDom.dll"
#r @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Visual Studio Tools for Office\PIA\Office14\Microsoft.Office.Interop.Excel.dll"
#r @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Visual Studio Tools for Office\PIA\Office14\Office.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.Rtd.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\log4net.dll"

open System
open System.Windows
open Microsoft.Office.Interop.Excel
open System.Runtime.InteropServices
open System.CodeDom.Compiler
open Microsoft.FSharp.Compiler.CodeDom
open Microsoft.FSharp.Control

let rawCode = """namespace FCellDemo
module MyUDF =
    let fAdd2 x y = x + y + 2.
"""

let code = String.Join("\r\n", rawCode.Split([|"\n"|], StringSplitOptions.RemoveEmptyEntries))
let provider = new FSharpCodeProvider()
let output = @"C:\Program Files (x86)\Statfactory\FCell 1.0\FSharpUdfs.dll"

let opt = new CompilerParameters(
                [|
                    "System.dll";
                    @"C:\Program Files (x86)\Statfactory\FCell 1.0\log4net.dll";
                    @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.dll";
                    @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.Rtd.dll";
                    @"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0\FSharp.Core.dll"|], output)

let res = provider.CompileAssemblyFromSource(opt, code)

if res.Errors.Count = 0 then
    try
        let xllPath = @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.xll"
        let app = Marshal.GetActiveObject("Excel.Application"):?> Microsoft.Office.Interop.Excel.Application
        app.RegisterXLL(xllPath) |> ignore
    with 
        | e -> printfn "%s" e.Message
else
    printfn "%s" (res.Errors.Item(0).ErrorText)
        

    