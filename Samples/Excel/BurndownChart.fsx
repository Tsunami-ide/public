#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.FSharp.dll"
#load "ExcelCharts.fsx"

open System
open System.Net
open MathNet.Numerics.Random
open MathNet.Numerics.Distributions
open Microsoft.Office.Interop.Excel

module Colors = 
    let red  = BitConverter.ToInt32([|255uy;0uy;0uy;0uy|],0)
    let blue  = BitConverter.ToInt32([|0uy;0uy;255uy;0uy|],0)
    let green  = BitConverter.ToInt32([|0uy;255uy;0uy;0uy|],0)

type BurnDownChart = {
    title : string
    xAxis : string
    yAxis : string
    days  : int
    storyPoints : int
    remaining : int[]
}

[<AutoOpen>]
module BurnDown =
    let idealBurndown days storyPoints = Array.init (days + 1) (fun i -> max 0 (storyPoints - int (float i * float storyPoints / float days)))
  

    let private genSinWave (width:float) (offset:float) = Seq.initInfinite (fun i -> sin (((float i + offset )/ width) * 2. * Math.PI)) 

    /// slow, fast, slow
    let private sfs days = genSinWave (float days * 2.) 0. |> Seq.map (fun x -> (x * 2.))
    // fast, slow, fast
    let private fsf days = genSinWave (float days * 2.) (float days) |> Seq.map (fun x -> (x + 1.) * 2.)
    // fast, slow, fast, slow
    let private fsfs days = genSinWave (float days * 0.666) (float days * 0.2) |> Seq.map (fun x -> (x + 1.))

    let private scurveBurndown days storyPoints (f:int -> float seq) (weight:float) = 
        [|
            let avg = float storyPoints / float days
            yield!
                Normal.WithMeanVariance(avg, avg * 3.).Samples() 
                |> Seq.zip (f days)
                |> Seq.map (fun (x,y) -> x * (max y 0.) * weight)
                |> Seq.map int
                |> Seq.map (max 4)
                |> Seq.scan (fun state t -> state - t) storyPoints
                |> Seq.takeWhile (fun x -> x > 0)
            yield 0
        |]
    
    let slowFastSlow        (chart:BurnDownChart) = {chart with remaining = scurveBurndown chart.days chart.storyPoints sfs 0.8}
    let fastSlowFast        (chart:BurnDownChart) = {chart with remaining = scurveBurndown chart.days chart.storyPoints fsf 1.3}
    let fastSlowFastSlow    (chart:BurnDownChart) = {chart with remaining = scurveBurndown chart.days chart.storyPoints fsfs 1.0}
    let avereage            (chart:BurnDownChart) = 
        {chart with 
            remaining = 
                [|
                    let avg = float chart.storyPoints / float chart.days
                    yield! 
                        Normal.WithMeanVariance(avg, avg * 3.).Samples() 
                        |> Seq.map int
                        |> Seq.map (max 4)
                        |> Seq.scan (fun state t -> state - t) chart.storyPoints
                        |> Seq.takeWhile (fun x -> x > 0)
                    yield 0
                |]
        }
    let mutable private  excelChart = Option<Chart>.None
    let truncate (n:int) (chart:BurnDownChart) = {chart with remaining = chart.remaining |> Seq.truncate n |> Seq.toArray}
    let display (chart:BurnDownChart) =

        let plotData (chart:Chart) ys =
            for (xs,color, name) in ys do
                let seriesCollection = chart.SeriesCollection() :?> SeriesCollection
                let series = seriesCollection.NewSeries()
                series.MarkerForegroundColor <- color
                series.Name <- name
                series.Format.Line.ForeColor.RGB <- color
                series.Values <- xs
        
        let displayChart (bdc:BurnDownChart) (ec:Chart) =
            ec.HasTitle <- true
            ec.ChartTitle.Text <- bdc.title
            let xAxis = ec.Axes(XlAxisType.xlCategory, XlAxisGroup.xlPrimary) :?> Axis
            xAxis.HasTitle <- true
            xAxis.AxisTitle.Text <- bdc.xAxis
            let yAxis = ec.Axes(XlAxisType.xlValue, XlAxisGroup.xlPrimary) :?> Axis
            yAxis.HasTitle <- true
            yAxis.AxisTitle.Text <- bdc.yAxis
            [|
                idealBurndown bdc.days bdc.storyPoints, Colors.blue,"Ideal Burndown"
                bdc.remaining, Colors.red,"Remaining"
            |]
            |> plotData ec
            
        match excelChart with
        | Some(x) ->
            ExcelCharts.clear x
            displayChart chart x
        | None ->
            match ExcelCharts.NewChart() with
            | Some(y) -> 
                excelChart <- Some(y)
                y.ChartType <- XlChartType.xlLineMarkers    
                displayChart chart y
            | None -> () // no-op

module WidgetCo = 
    let moreHeadCountNeeded : BurnDownChart -> BurnDownChart = failwith "todo"
    let everythingIsFine : BurnDownChart -> BurnDownChart = failwith "todo"
    /// Demonstrate little and slowing progress
    let thisWasNotMyIdea : BurnDownChart -> BurnDownChart = failwith "todo"

            


let Default = {
    title = "Big Important Project Burn Down"   
    xAxis = "Iteration Timeline (days)"
    yAxis = "Sum of Task Estimates (Story Points)"
    days  = 20
    storyPoints = 200
    remaining = [||]
    }

Default |> slowFastSlow |> display
Default |> fastSlowFast |> truncate 10 |> display
Default |> fastSlowFastSlow |> display
Default |> avereage |> display
