(* NOTE: Run in a seperate FSI Shell *)
#load "shared.fsx"
#load "..\Utilities\WebData.fsx"
open Tsunami.Public
#r "PresentationCore"
#r "PresentationFramework"
#r "WindowsBase"
#r "System.Xaml"
#r "UIAutomationTypes"
open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text
open System.Windows
open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Markup
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Media3D
open System.Windows.Media.Imaging
open System.Windows.Threading
open System.Xml
open System.Diagnostics
open System.Threading
open System.Xaml
open Shared

let window = Viewer(Topmost = true)
window.Show()

let add mv3d =  window.Viewport.Children.Add(mv3d); mv3d
let remove mv3d = window.Viewport.Children.Remove(mv3d) |> ignore

let applyMatrix matrix (mv3D:Visual3D) = mv3D.Transform <- MatrixTransform3D(matrix); mv3D

let updateMatrix (f:Matrix3D -> Matrix3D) (mv3D:Visual3D) =     
    match mv3D.Transform with
    | :? MatrixTransform3D as mx -> mv3D.Transform <- MatrixTransform3D(f(mx.Matrix))
    | _ -> mv3D.Transform <- MatrixTransform3D(f(Matrix3D.Identity))
    mv3D

let addMatrix (m:Matrix3D) mv3D = updateMatrix (fun x -> x * m) mv3D

open Vector3D
open Matrix3D

let translate x y z (mv3D:Visual3D) = addMatrix ((translate(Vector3D(x,y,z)))) mv3D
let scale x y z (mv3D:Visual3D) = addMatrix (scale(Vector3D(x,y,z))) mv3D
let reset (mv3D:Visual3D) = applyMatrix unitM mv3D
let rotate q = addMatrix(Matrix3D.rotate q)

let imageModel url model  = 
        let bi = BitmapImage()
        bi.BeginInit()
        bi.StreamSource <- new MemoryStream(WebData.ReadAllData(@"http://tsunami.io/assets/skypic_small.jpg"))
        bi.EndInit()
        let material = DiffuseMaterial(ImageBrush(bi))
        ModelVisual3D(Content = GeometryModel3D(Geometry = (model), Material = material, BackMaterial = material))

let brushModel brush model = ModelVisual3D(Content = GeometryModel3D(Geometry = (model), BackMaterial = (DiffuseMaterial(brush)), Material = (DiffuseMaterial(Brushes.Goldenrod))))



let terrain = 
    square() 
    |> imageModel @"http://tsunami.io/assets/skypic_small.jpg"
    |> scale 200. 200. 200.
    |> rotate (Quaternion(XAxis,90.))
    |> translate 1. -1. 1.
    |> add


let walkerXaml =  WebData.ReadAllText(@"http://tsunami.io/assets/walker.xaml")
let makeWalker() = walkerXaml |> parseXAML :?> ModelVisual3D


let walker = makeWalker() |> add 
let cube =  cube() |> brushModel Brushes.Goldenrod |> add
