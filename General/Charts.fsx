#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "System.Xaml.dll"
#r "Telerik.Windows.Data.dll"
#r "Telerik.Windows.Controls.dll"
#r "Telerik.Windows.Controls.Charting.dll"
#r "Telerik.Windows.Controls.RibbonView.dll"
#r "Telerik.Windows.Controls.Docking.dll"
#r "Telerik.Windows.Controls.Navigation.dll"
#r "ActiproSoftware.SyntaxEditor.Wpf.dll"
#r "ActiproSoftware.Shared.Wpf.dll"
#r "ActiproSoftware.Text.Wpf.dll"
#r "ActiproUtilities.dll"
#r "Tsunami.IDEDesktop.exe"
#r "UIAutomationTypes.dll"

open System
open System.Windows
open System.Windows.Data
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Shapes
open Telerik.Windows.Controls
open Telerik.Windows.Controls.Charting

let ui = System.Windows.Threading.DispatcherSynchronizationContext(Tsunami.IDE.UI.Instances.ApplicationMenu.Dispatcher)

let chart =
    async {
        do! Async.SwitchToContext ui
        let chart = RadChart()
        Tsunami.IDE.UI.Instances.VisualizationPane.IsHidden <- false
        Tsunami.IDE.UI.Instances.VisualizationPane.Content <-  chart
        return chart
    } |> Async.RunSynchronously

type Candle =
    struct
        val date : DateTime
        val high : float
        val low : float
        val ``open`` : float
        val ``close`` : float
        new(d: DateTime, h: float, l: float, o: float, c: float) = 
            { 
                date = d
                high = h
                low = l
                ``open`` = o
                ``close`` = c
            }
    end

module Chart =
    let lines (xss:(string*float[])[]) = 
            async {
                do! Async.SwitchToContext ui
                for (name,data) in xss do
                    let lineSeries = DataSeries(LegendLabel = name, Definition = new LineSeriesDefinition())
                    lineSeries.Definition.Appearance.PointMark.Fill <- System.Windows.Media.Brushes.Transparent
                    lineSeries.Definition.Appearance.PointMark.Stroke <- System.Windows.Media.Brushes.Transparent
                    lineSeries.Definition.ShowItemLabels <- false
                    for x in data do
                        lineSeries.Add(DataPoint(x))
                    chart.DefaultView.ChartArea.DataSeries.Add(lineSeries)
            } |> Async.RunSynchronously

    let clear() = 
            async {
                do! Async.SwitchToContext ui
                chart.DefaultView.ChartArea.DataSeries.Clear()
            } |> Async.RunSynchronously


    let candleStick (name:string, xs:Candle[]) = 
            async {
                do! Async.SwitchToContext ui
                chart.DefaultView.ChartArea.AxisX.IsDateTime <- true
                chart.DefaultView.ChartArea.AxisX.LayoutMode <- AxisLayoutMode.Inside
                chart.DefaultView.ChartArea.AxisX.LabelRotationAngle <- 45.
                chart.DefaultView.ChartArea.AxisX.DefaultLabelFormat <- "dd-MMM"

                let candleStickSeries = DataSeries(LegendLabel = name, Definition = new CandleStickSeriesDefinition())
                candleStickSeries.AddRange(xs |> Array.map (fun x -> DataPoint(High = x.high, Low = x.low, Open = x.``open``, Close = x.close, XValue = x.date.ToOADate())))
                chart.DefaultView.ChartArea.DataSeries.Add(candleStickSeries) 
            } |> Async.RunSynchronously
        

let random = new System.Random()
let randomWalk() = [|0..20|] |> Array.scan (fun state _ -> state + random.NextDouble() * 2. - 1.) 0.
let randomWalks = [| for i in 0..5 -> ("Random Walk " + string i, randomWalk()) |]

Chart.lines randomWalks

Chart.clear()

[|
    let now = DateTime.Now
    for i in 0..20 ->
        let o = random.NextDouble()
        let c = random.NextDouble()
        let h = (max o c) + random.NextDouble() / 4.
        let l = (min o c) - random.NextDouble() / 4.
        Candle(now.AddDays(float i),h,l,o,c)
|] |> fun xs -> Chart.candleStick("ASX200",xs)