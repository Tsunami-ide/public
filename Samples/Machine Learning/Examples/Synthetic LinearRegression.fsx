#load @"../../Utilities/Utilities.fsx"
#load @"../LinearRegression.fsx"
#r "Tsunami.IDEDesktop.exe"
#r "System.Data.dll"
#r "System.Drawing.dll"
#r "System.Windows.Forms.DataVisualization.dll"
#r "System.Windows.Forms.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.FSharp.dll"

open Tsunami.IDE.FSharp.Charting
open MathNet.Numerics.LinearAlgebra.Double
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.Statistics
open MathNet.Numerics.Random
open MathNet.Numerics.Distributions
open Tsunami.Public.Utilities
open Tsunami.Public.MathNet
open System.IO
open System

do System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

open LinearRegression

let (X,y) =
    let xs = 
        let r = System.Random()
        Array.init 100 (fun _ -> Math.Pow(r.NextDouble(), 3.) * 20.)
        
    let ys = 
        let normal = Normal.WithMeanVariance(5.0, 2.0)
        xs |> Array.map (fun x -> 0.7 * x + normal.Sample())
        
    let X = [ Array.create xs.Length 1. ; xs ] |> Matrix.ofSeqCols
    let y = [ ys ] |> Matrix.ofSeqCols
    X, y

// Plot the data
let plotPoints() =  Seq.zip (X.Column(1)) (y.Column(0)) |> Chart.FastPoint
plotPoints ()

// Check the cost with an initial guess of zeros (should be ~32)
let initialTheta = Matrix.zeros X.ColumnCount y.ColumnCount
computeCost (X, y, initialTheta)

// Run the gradient descent
let iterations = 1500
let alpha = 0.01
let (Js, Thetas) = 
    regression X y initialTheta alpha
    |> Seq.take iterations
    |> Seq.toArray
    |> Array.unzip

// Calculated coefficients, local minimum found by gradient descent
let theta = Seq.last Thetas

// Sanity check predictions
let predict(x:float list) = (Matrix.ofSeqRows [x] * theta).[0,0]
predict [1.;3.5]
predict [1.;7.]

// Plot the regression line
let plotPointsAndLine() =
    let min = 5.0
    let max = 25.0
    let line = [|(min,predict([1.;min]));(max,predict([1.;max]))|]
        
    let c = (Chart.Combine [ Chart.Line(line); plotPoints() ])
    c.AndXAxis(Min=min,Max=max)
    
let plotCost() = Chart.FastPoint(Seq.zip [1..Js.Length] Js)

plotPointsAndLine()