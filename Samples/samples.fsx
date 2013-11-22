#r "Tsunami.IDEDesktop.exe"
#r "Ionic.Zip.dll"
#r "FSharp.Compiler.dll"
#r "Newtonsoft.Json.dll"
// This will warm up the cache
do
    [|
        "cache:http://tsunami.io/assemblies/MathNet.Numerics.dll"
        "cache:http://tsunami.io/assemblies/MathNet.Numerics.FSharp.dll"
        "cache:http://tsunami.io/assemblies/office.dll"
        "cache:http://tsunami.io/assemblies/Microsoft.Office.Interop.Excel.dll"
        "cache:http://tsunami.io/assemblies/FSharp.Data.TypeProviders.dll"
        "cache:http://tsunami.io/assemblies/FCell.XlProvider.dll"
        "cache:http://tsunami.io/assemblies/FunScript.dll"
        "cache:http://tsunami.io/assemblies/FunScript.TypeScript.dll"
        "cache:http://tsunami.io/assemblies/FunScript.TypeScript.Interop.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.Extension.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.Extension.Graphics.Direct3D9.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.Direct3D9.dll"
        "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.RawInput.dll"
        "cache:http://tsunami.io/assemblies/SharpVectors/SharpVectors.Converters.dll"
        "cache:http://tsunami.io/assemblies/SharpVectors/SharpVectors.Runtime.dll"
        "nuget://Fantomas/1.0.0/lib/Args.dll"
        "nuget://Fantomas/1.0.0/lib/FantomasLib.dll"
        "nuget://FSharp.Compiler.Editor/1.0.5/lib/net40/FSharp.Compiler.Editor.dll"
        "nuget://MathNet.Numerics/2.5.0/lib/net40/MathNet.Numerics.dll"
        "nuget://MathNet.Numerics.FSharp/2.5.0/lib/net40/MathNet.Numerics.FSharp.dll"
    |]
    |> Array.iter (fun x -> 
        printfn "preparing cache %s" x
        Tsunami.FS.FileSystem.fileSystem.SafeExists(x) |> ignore)
    |> ignore

[<AutoOpen>]
module FileHelpers =
    
    open Tsunami.IDE
    open Tsunami.IDE.TypeProvider
    open Tsunami.IDE.Model
    open Tsunami.IDE.Projects
    
    [<Literal>]
    let root = __SOURCE_DIRECTORY__
        
    type Files = DirectoryTypeProvider<root>
    
    let file path = FS.File path
    let dir name files = FS.Directory(name, files)
    
    let createProject name content code = 
        ProjectsViewModel.Instance.RemoveByName name
    
        let project =
            { FSharpProject.DebugBuild with
                name = name
                build = ignore
                references = [| "System.dll"; "System.Net.dll" |]
                resources = content
                code = code }
                
        ProjectsViewModel.Instance.Add project
        
    module CSharp =
        let createProject name content code =
            ProjectsViewModel.Instance.RemoveByName name
            { CSharpProject.Empty with
                name = name
                build = ignore
                //references = [| "System.dll"; "System.Net.dll" |]
                resources = content
                code = code }
            |> ProjectsViewModel.Instance.Add 
                
        
    module VB =
        let createProject name content code =
            ProjectsViewModel.Instance.RemoveByName name
            { VBProject.Empty with
                name = name
                build = ignore
                //references = [| "System.dll"; "System.Net.dll" |]
                resources = content
                code = code }
            |> ProjectsViewModel.Instance.Add
    


[<AutoOpen>]
module EventStore =
    
    
    type Root = Files.EventStore
    
    do createProject "EventStore" [||] [|
        file Root.``EventStore.fsx``
        file Root.``EventStoreTest.fsx``
        //file Root.``EventStoreTestSL.fsx``
    |]

[<AutoOpen>]
module MachineLearning =
    type Root = Files.``Machine Learning``
    type Examples = Root.Examples

    do createProject "Machine Learning" [||] [|
        dir "Examples" [|
            file Examples.``Digits Nearest Neighbour.fsx``
            file Examples.``Digits Random Forest.fsx``
            file Examples.``Iris KMeans.fsx``
            file Examples.``Synthetic LinearRegression.fsx``
        |]
        file Root.``KMeans.fsx``
        file Root.``KNearestNeighbours.fsx``
        file Root.``LinearRegression.fsx``
        file Root.``LogisticRegression.fsx``
        file Root.``MathNet.fsx``
        file Root.``NeuralNetwork.fsx``
        file Root.``RandomForest.fsx``
    |]
    
[<AutoOpen>]
module _3DViewport =
    do createProject "3D Viewport" [||] [|
        file Files.``3D``.``scene.fsx``
        file Files.``3D``.``geometry.fsx``
        file Files.``3D``.``shared.fsx``
    |]    

[<AutoOpen>]
module Excel =
    
    type Root = Files.Excel
    type Stocks = Root.Stocks
    type UDFs = Root.UDFs
    type Widget = Root.Widget
    type ZeroInstall = Root.``Zero Install``
    
    do createProject "Excel"
        [|
            file Root.``README.md``
            dir "Widget" [|file Widget.``Widget.xlsx``|]
            
        |]
        [|
            file Root.``ABTesting.fsx``
            file Root.``BurndownChart.fsx``
            file Root.``ExcelCharts.fsx``
            
            dir "Stocks" [|
                file Stocks.``Demo.fsx``
                file Stocks.``StocksScript.fsx``
            |]
            
            dir "Widget" [| file Widget.``WidgetDemo.fsx`` |]
            // TODO: Copy settings from this file!
            // file ZeroInstall.``ExcelDemoSetup.fsx``
            dir "Zero Install" [| file ZeroInstall.``ExcelDemo.fs`` |]
        |]
        
    do CSharp.createProject "Excel.CSharp"
        [| dir "UDFs" [| file UDFs.``Nasdaq100.xlsx`` |] |]
        [| 
            dir "UDFs" [| file UDFs.``CSharp.cs`` |] 
            dir "Zero Install" [| file ZeroInstall.``ExcelDemo.cs`` |]
        |]
        
    do VB.createProject "Excel.VB"
        [|
            dir "UDFs" [|
                file UDFs.``Nasdaq100.xlsx``
            |]
        |]
        [|
            dir "UDFs" [|
                file UDFs.``VisualBasic.vb``
            |]
            
            dir "Zero Install" [|
                file ZeroInstall.``ExcelDemo.vb``
            |]
        |]


[<AutoOpen>]
module FunScript =
    
    type Root = Files.FunScript
    
    do createProject "FunScript" [||] [|
        file Root.``ChatServer.fsx``
        file Root.``ChatClient.fsx``
    |]


[<AutoOpen>]
module Games =
    
    type Root = Files.Games
    
    do createProject "Games" [||] [|
        file Root.``MissileCommand.fsx``
        file Root.``Pacman.fsx``
        file Root.``Tetris.fsx``
        file Root.``Games.fsx``
    |]


[<AutoOpen>]
module GPGPU =
    
    type Root = Files.GPGPU
    
    do createProject "GPGPU" [||] [|
        file Root.``WaveSurfacePlotting.fsx``
        file Root.``SimpleTest.fsx``
    |]


[<AutoOpen>]
module Hive =
    
    type Root = Files.Hive
    
    do createProject "Hive" [||] [|
        file Root.``TsunamiHiveDemo.fsx``
    |]


[<AutoOpen>]
module Langauges =
    
    type Root = Files.Languages
    
    do createProject "Languages" 
        [|
            file Root.``Assembly.asm``
            file Root.``BatchFile.bat``
            file Root.``C.c``
            file Root.``C++.cpp``
            file Root.``CSharp.cs``
            file Root.``CSS.css``
            file Root.``Ebnf.el``
            file Root.``FSharp.fsx``
            file Root.``HTML.html``
            file Root.``IniFile.ini``
            file Root.``Java.java``
            file Root.``JavaScript.js``
            file Root.``Lua.lua``
            file Root.``MarkDown.md``
            file Root.``MSIL.msil``
            file Root.``Pascal.pas``
            file Root.``Perl.pl``
            file Root.``PowerShell.ps``
            file Root.``Python.py``
            file Root.``Rtf.rtf``
            file Root.``Ruby.rb``
            file Root.``SQL.sql``
            file Root.``Text.txt``
            file Root.``VB.vb``
            file Root.``VBScript.vbs``
            file Root.``XAML.xaml``
            file Root.``XML.xml``
        |] [||]



[<AutoOpen>]
module MBrace =
    
    type Root = Files.MBrace
    
    do createProject "MBrace" [||] [|
        file Root.``Demo.fsx``
        file Root.``Setup.fsx``
    |]

#r "System.Core.dll"
#load "PhoneGap/PhoneGap.fs"

[<AutoOpen>]
module PhoneGapProject =
    
    /// Bring IDE and type providers into scope.
    open Tsunami.IDE
    open Tsunami.IDE.TypeProvider
    open System.IO
    open Tsunami
    open Tsunami.IDE.Projects
    open Tsunami.IDE.Model
    
    [<Literal>]
    let root = __SOURCE_DIRECTORY__
    type Root = DirectoryTypeProvider<root>.PhoneGap
    
    // Insert your authentication token here.
    // Login and find it at https://build.phonegap.com/people/edit
    let token = ""
    
    // Uncomment the following to create your app.
    // Use the returned id to populate `appid`
    
    //PhoneGap.Write.createApp(
    //    token,
    //    "Sample App",
    //    PhoneGap.Bytes (PhoneGap.zipDirToBytes (Path.Combine (__SOURCE_DIRECTORY__, "www")),
    //                    "app.zip"),
    //    ``private``=false)
       
    // Insert your app id here
    let appid = 0
        
    let deployToPhoneGapBuild _ =
        async {
            let bs = PhoneGap.zipDirToBytes Root.www.Path
            PhoneGap.Write.updateApp (token, appid, (bs, "app.zip")) |> ignore
        }
        |> Async.Start
    
    let phoneGapSampleProject =
        {   FSharpProject.DebugBuild
            with
                name = "PhoneGap Build"
                build = deployToPhoneGapBuild
                code = 
                    [|
                        FS.File Root.www.``config.xml``
                        FS.File Root.www.``index.html``
                        FS.File Root.www.``spec.html``
                    |]
        }
    
    ProjectsViewModel.Instance.RemoveByName phoneGapSampleProject.name
    ProjectsViewModel.Instance.Add phoneGapSampleProject
    


[<AutoOpen>]
module FantomasProject =

    open Tsunami.IDE
    open Tsunami.IDE.Projects
    open Tsunami.IDE.Model
    
    type Root = Files.Plugins.Fantomas
    
    let fantomasPluginProject =
        {   FSharpProject.DebugBuild
            with
                name = "Fantomas Plugin"
                build = Build.fsharpProjectAndCopyLocal
                localreferences = [|
                                        @"nuget://Fantomas/1.0.0/lib/Args.dll"
                                        @"nuget://Fantomas/1.0.0/lib/FantomasLib.dll"
                                        @"nuget://FSharp.Compiler.Editor/1.0.5/lib/net40/FSharp.Compiler.Editor.dll"
                                   |]
                references =
                        [|
                            "Tsunami.IDEDesktop.exe"
                            "PresentationFramework.dll"
                            "PresentationCore.dll"
                            "WindowsBase.dll"
                            "System.dll"
                            "System.Xaml.dll"
                         |]
                code =
                    [|
                        file Root.``Main.fs``
                    |] 
                out = System.IO.Path.Combine
                        [|
                            System.Environment.SpecialFolder.MyDocuments
                            |> System.Environment.GetFolderPath
                            "Tsunami"
                            "Plugins"
                            "FantomasPlugin.dll"
                        |]
                      |> Some
        }
       
    ProjectsViewModel.Instance.RemoveByName fantomasPluginProject.name
    ProjectsViewModel.Instance.Add fantomasPluginProject


[<AutoOpen>]
module Rhino =
    
    type Root = Files.``Rhino 3D``
    
    do createProject "Rhino 3D" 
        [|
            file Root.``README.md``
        |] 
        [|
            file Root.``Circles On Sphere.fsx``
            file Root.``Sierpinski Triangles.fsx``
        |]


[<AutoOpen>]
module Turtle =
    
    type Root = Files.Turtle
    
    do createProject "Turtle" [||] [|
        file Root.``TurtleSVG.fsx``
        file Root.``TurtleExamples.fsx``
    |]


[<AutoOpen>]
module TypeProviders =
    
    type Root = Files.``Type Providers``
    
    do createProject "Type Providers" [||] [|
        file Root.``DocumentTypeProvider.fsx``
        file Root.``NuGetTypeProvider.fsx``
        file Root.``S3TypeProvider.fsx``
        file Root.``FacebookTypeProvider.fsx``
    |]

