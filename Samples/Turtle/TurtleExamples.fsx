module Tsunami.Public.Turtle.TurtleExamples
#r "Tsunami.IDEDesktop.exe"
#r "System.Xml.Linq"
#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "System.Xaml.dll"

#r "cache:http://tsunami.io/assemblies/SharpVectors/SharpVectors.Converters.dll"
#r "cache:http://tsunami.io/assemblies/SharpVectors/SharpVectors.Runtime.dll"

#load "TurtleSVG.fsx"
open System.Xml.Linq
open System
open System.IO
open System.Windows
open Tsunami.Public.Turtle.TurtleSVG

let repete (count:int) (xs:instruction list) = [for _ in 0..count do yield! xs]
let square (size:float) = repete 4 [move size; turn 90.]

let display(xs:instruction list) = 
    let svg = 
        xs
        |> runDefault 
        |> toSVG(false) 
    Tsunami.IDE.SimpleUI.addControlToNewDocument("SVG",fun _ -> 
        let viewbox = SharpVectors.Converters.SvgViewbox()
        let scrollViewer = new System.Windows.Controls.ScrollViewer(Content = viewbox)
        // TODO - remove dependency on File System
        let tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        svg.Save(tmpFile)
        viewbox.Source <- new Uri(tmpFile)        
        scrollViewer :> UIElement)
    

[
    yield move 100.
    yield turn 90.
    yield move 100.
] |> display

let fancySquares() =
    [
        yield width 3.
        yield color red
        yield down
        for i in 20 .. 10 .. 100 do
        yield color (getRandomColor())
        yield! square (float i)
    ] 
    
let filledSquare() =
    [
        yield width 3.
        yield color blue
        yield fill red
        yield! repete 3 [move 100.; turn 90.]
        yield close
        yield fillNone
    ] 

let left x = [up;turn 90.;move x;turn -90.;down]
let right x = [up;turn -90.;move x;turn 90.;down]
let rec drawTree distance =
    let delta = 10.
    let angle = 30.
    if distance < 0. 
    then []
    else
        [
            yield move distance
            yield turn angle
            yield! drawTree (distance - delta)
            yield turn (-angle * 2.)
            yield! drawTree (distance - delta)
            yield turn angle
            yield move -distance
        ]    

let garden() =
    [
        yield color green
        for i in 10..10..40 do
            let i' = float i  * 2.
            yield! right i'
            yield! drawTree i'
        yield color brown
        for i in 10..10..40 do
            let i' = float i  * 2.
            yield! left i'
            yield! drawTree 30.
    ]
    
let shape sides size =
    let angle = 360. / float sides
    let length = size / float sides
    [for i in 1 .. sides do yield! [move length; turn angle]]

let multipleCircles() =
    [
        yield width 3.
        yield color blue
        for _ in 1..20 do
            yield! shape 50 400.
            yield turn 18.
    ]

fancySquares()      |> display
filledSquare()      |> display
garden()            |> display
multipleCircles()   |> display