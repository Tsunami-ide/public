(*
    INSTRUCTIONS
    
    Run this script in Tsunami embedded in Excel
    Clear the script and add the UDF code, e.g.

    """namespace FCellDemo
    module MyUDF =
        let fAdd2 x y = x + y + 2."""

    Compile by clicking on Ribbon -> Excel -> Common -> Compile button

    Run the UDF function in an Excel cell, e.g. "=fadd2(1,2)" 

*)

#r "Tsunami.IDEDesktop.exe"
#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "Telerik.Windows.Controls.dll"
#r "Telerik.Windows.Controls.Navigation.dll"
#r "Telerik.Windows.Controls.Docking.dll" 
#r "Telerik.Windows.Controls.RibbonView.dll" 
#r "ActiproSoftware.SyntaxEditor.Wpf.dll"
#r "System.Xaml.dll"
#r "ActiproSoftware.Shared.Wpf.dll"
#r "ActiproSoftware.Text.Wpf.dll"
#r "ActiproUtilities.dll"
#r "FSharp.Compiler.dll"

#r @"C:\Program Files (x86)\FSharpPowerPack-4.0.0.0\bin\FSharp.Compiler.CodeDom.dll"
#r @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Visual Studio Tools for Office\PIA\Office14\Microsoft.Office.Interop.Excel.dll"
#r @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Visual Studio Tools for Office\PIA\Office14\Office.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.Rtd.dll"
#r @"C:\Program Files (x86)\Statfactory\FCell 1.0\log4net.dll"


open Tsunami.IDE
open Tsunami.Utilities
open System
open System.Windows
open System.Windows.Controls
open ActiproSoftware.Windows.Controls
open Telerik.Windows.Controls
open Telerik.Windows.Controls.RibbonView
open Tsunami.CC.SourceServices
open Tsunami.FS.FileSystem
open Microsoft.Office.Interop.Excel
open System.Runtime.InteropServices
open System.CodeDom.Compiler
open Microsoft.FSharp.Compiler.CodeDom
open Microsoft.FSharp.Control


let ui = Threading.DispatcherSynchronizationContext(Tsunami.IDE.UI.Instances.ApplicationMenu.Dispatcher)

let compile() =
    let ui = Threading.DispatcherSynchronizationContext(Tsunami.IDE.UI.Instances.ApplicationMenu.Dispatcher)
    let reloadFcell() =
        let xllPath = @"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.xll"
        let app = Marshal.GetActiveObject("Excel.Application"):?> Microsoft.Office.Interop.Excel.Application
        app.RegisterXLL(xllPath) |> ignore

    async {
        do! Async.SwitchToContext ui
        let rawCode = Tsunami.FS.FileSystem.fileSystem.ReadAllText("ms:Shell.fsx")
        let codeArr = rawCode.Split([|"\n"|], StringSplitOptions.RemoveEmptyEntries)
                         |> Array.filter (fun s -> not (s.Contains("#r")))
        let code = String.Join("\r\n", codeArr)
        let provider = new FSharpCodeProvider()
        let output = @"C:\Program Files (x86)\Statfactory\FCell 1.0\FSharpUdfs.dll"
        let opt = new CompilerParameters([|"System.dll";@"C:\Program Files (x86)\Statfactory\FCell 1.0\log4net.dll";@"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.dll";@"C:\Program Files (x86)\Statfactory\FCell 1.0\FCell.ManagedXll.Rtd.dll";@"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0\FSharp.Core.dll"|], output)
        let res = provider.CompileAssemblyFromSource(opt, code)
        if res.Errors.Count = 0 then
            try
                reloadFcell()
            with 
                | e -> MessageBox.Show(e.Message) |> ignore
        else
            MessageBox.Show(res.Errors.Item(0).ErrorText) |> ignore
        
    } |> Async.Start
    
async {
    do! Async.SwitchToContext ui
    let button = RadRibbonButton(Text = "Compile", Size = ButtonSize.Large) 
    button.Click.Add(fun _ -> compile())
    let excelTab =
        [|
            [| button |]
            |> addItems (RadRibbonGroup(Header = "Common"))
        |] |> addItems (RadRibbonTab(Header = "Excel"))    
    Tsunami.IDE.UI.Instances.RibbonView.Items.Add(excelTab) |> ignore
    
} |> Async.RunSynchronously