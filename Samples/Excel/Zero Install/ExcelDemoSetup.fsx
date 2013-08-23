#r "Tsunami.IDEDesktop.exe"
open System
open System.IO
open Tsunami.IDE

let demoDir = __SOURCE_DIRECTORY__

(* Zero Install Excel FCell components are not yet publically available *)
(*
#r @"NOT RELEASED YET\FCell.Packaging.dll"
open FCell.Packaging.Packager
let root = @"NOT RELEASED YET"
let generate ouput (dlls:string[]) =
    File.Delete(output)
    File.Copy(root + "Packaged.xlsm.bak", output)
    
    packageUDFs ([|
                    yield! 
                        [|
                          "FCell.Bootstrap.xll"
                          "FCell.ManagedXll.dll"
                          "FCell.ManagedXll.Rtd.dll"
                        |] |> Array.map ((+) root)
                    yield! dlls |> Array.filter (fun x -> x <> demoDir + "VBExcelDemo.dll")
                    yield @"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0\FSharp.Core.dll"
                    yield @"VBExcelDemo.dll"
                |])
                output
*)

#r @"C:\Program Files (x86)\Open XML SDK\V2.0\lib\DocumentFormat.OpenXml.dll"

let output = demoDir + "Demo.xlsm"

let buildCS = Build.csharpProject
let buildVB = Build.csharpProject



let fsProject = { FSharpProject.DebugBuild with references = [| yield @"System.dll"; yield @"System.Net.dll"; yield! FSharpProject.DebugBuild.references |]; name = "Excel F#";  code = [| FS.File(demoDir + "ExcelDemo.fs") |]; out = Some(demoDir + "FSExcelDemo.dll") }     
let cSharpProject = { CSharpProject.Empty with name = "Excel C#"; code =  [| FS.File(demoDir + "ExcelDemo.cs") |]; out = Some(demoDir + "CSExcelDemo.dll")  }
let vbProject = { VBProject.Empty with name = "Excel VB"; code =  [| FS.File(demoDir + "ExcelDemo.vb") |]; out = Some(demoDir + "VBExcelDemo.dll")  }

(* Add projects to Project View *)
let addProjects() =
    ProjectsViewModel.Instance.Add(fsProject)
    [cSharpProject; vbProject] |> Seq.iter ProjectsViewModel.Instance.Add

(* Build Projects *)
let buildAndDeploy() =
    Build.fsharpProject(fsProject)
    fsProject.build(fsProject)
    buildCS(cSharpProject)
    buildVB(vbProject)

    (* Embed into Excel Workbook *)
    [| "FSExcelDemo.dll"; "CSExcelDemo.dll"; "VBExcelDemo.dll"|] 
    |> Array.map ((+) demoDir)
    |> generate (demoDir + "Demo.xlsm")









