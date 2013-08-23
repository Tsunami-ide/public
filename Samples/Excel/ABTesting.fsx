#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.FSharp.dll"
#load "ExcelCharts.fsx"

open System
open System.Net
open MathNet.Numerics.Random
open MathNet.Numerics.Distributions
open Microsoft.Office.Interop.Excel

let now = System.DateTime.Now
let chart = ExcelCharts.NewChart().Value    
let clear = ExcelCharts.clear

chart.ChartType <- XlChartType.xlLine

let monthOfYearAdjustment = 
    let adjustments =
        [|
                    yield 1.3 // January
                    yield 0.8 // Feburary
                    yield 1.0 // March
                    yield 0.9  // April
                    yield 0.7 // May
                    yield 0.4 // June
                    yield 0.4 // July
                    yield 0.7 // August
                    yield 1.0 // September
                    yield 1.3 // October
                    yield 2.0 // November 
                    yield 2.5 // December
        |] 
    let average = (adjustments |> Array.sum) / float adjustments.Length
    fun (time:System.DateTime) -> 
        
        // TODO - find a better interpolation formulae
        let current = adjustments.[time.Month - 1] / average
        let previous = adjustments.[(time.Month - 2 + 12) % 12] / average
        let numberOfDays = float (DateTime.DaysInMonth(time.Year,time.Month))
        let day = float time.Day
        (previous * ((numberOfDays - day) / numberOfDays) + current * (day / numberOfDays)) / 2.
        
        
        
let dayOfWeekAdjustment =
    let adjustments =
        [|
                    yield 1.5 // Sunday
                    yield 0.6 // Monday
                    yield 0.6 // Tuesday
                    yield 0.8 // Wednesday
                    yield 0.6 // Thursday
                    yield 0.8 // Friday
                    yield 2.0 // Saturday
        |]
    let average = (adjustments |> Array.sum) / float adjustments.Length
    fun (time:System.DateTime) -> adjustments.[int time.DayOfWeek] / average
    

let normal = Normal.WithMeanVariance(0.0, 1.0)

let weightedFunction(time:DateTime) = 
        monthOfYearAdjustment(time) * 10. + 
        dayOfWeekAdjustment(time) * 2. + 
        normal.Sample() * 0.4

let red  = BitConverter.ToInt32([|255uy;0uy;0uy;0uy|],0)
let blue  = BitConverter.ToInt32([|0uy;0uy;255uy;0uy|],0)
let green  = BitConverter.ToInt32([|0uy;255uy;0uy;0uy|],0)

let genData n = 
    let gen() = [| for x in 0..n -> now.AddDays(float -x) |] |> Array.map (weightedFunction)
    let xs = 
        [|
            // Controls
            for i in 1..5 -> (gen(), red, sprintf "Cntrl%i" i)
            // Experiments
            for i in 1..4 -> (gen(), blue, sprintf "Exp%i" i) 
            // Successful Experiment
            yield (gen() |> Array.map ((+) 2.), blue, "Exp5")
        |] 
    [|
        yield! xs
        yield ([| for i in 0..n -> [| for j in 0..xs.Length-1 -> match xs.[j] with | (xs,color,name) -> xs.[i] |] |> Array.average |], green, "Average")
    |]
    
let plotData (chart:Chart) ys =
    for (xs,color, name) in ys do
        let seriesCollection = chart.SeriesCollection() :?> SeriesCollection
        let series = seriesCollection.NewSeries()
        series.Name <- name
        series.Format.Line.ForeColor.RGB <- color
        series.Values <- xs
        
clear chart
genData 28 |> plotData chart
clear chart
genData 365 |> plotData chart
