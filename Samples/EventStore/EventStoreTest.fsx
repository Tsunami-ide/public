// To run this sample you must run Tsunami as an administrator.
// Please download and install EventStore before you run this sample.
// You can find it here: http://geteventstore.com/
// Make sure you update the value of the eventStoreDir binding to your install location.

#r "Tsunami.IDEDesktop.exe"
#r "Newtonsoft.Json.dll"
#r "System.Reactive.Core.dll"
#r "System.Reactive.Linq.dll"
#r "System.Reactive.Interfaces.dll"
#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml.dll"
#r "System.Core.dll"
#r "System.Xml.Linq.dll"
#r "UIAutomationTypes.dll"
#r "System.Drawing.dll"
#r "System.Windows.Forms.dll"
#r "WindowsFormsIntegration.dll"
#r "System.Windows.Forms.DataVisualization.dll"
#r "ActiproSoftware.Charts.Wpf.dll"
#r "ActiproSoftware.Shared.Wpf.dll"
#load "EventStore.fsx"


//let policyServer = Tsunami.Server.PolicyServer.server System.Net.IPAddress.Loopback 943
let eventStoreDir = @"C:\bin\EventStore\eventstore-net-2.0.0"

open Tsunami
open Tsunami.Utilities

open System
open System.IO
open System.Net
open System.Text
open System.Collections.Generic
open Tsunami.Utilities
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Diagnostics
open ActiproSoftware.Windows.Controls.Charts
open System.Windows
open System.Windows.Controls
open System.Reactive.Linq

let eventStoreExe = 
    let path = Path.Combine(eventStoreDir, "EventStore.SingleNode.exe")
    if not (File.Exists path) then 
        failwith "Please install EventStore. See comment at top of page."
    path

let eventStore = Process.Start(eventStoreExe, "--http-port=4532")

let stream = "http://127.0.0.1:4532/streams/floatstream"

EventStore.postData stream 4.0 |> ignore

let obs = EventStore.observableOfStream<float> stream |> fst 
let d = obs.Subscribe(fun (dt,v) -> printfn "Received %s %f" (dt.ToString()) v) 

d.Dispose()

let chatStream = "http://127.0.0.1:4532/streams/chatstream"

type Chat = {
    sender : string
    message : string
}

EventStore.postData chatStream {sender = "Matt"; message = "Hello World"} |> ignore
let chatObs = EventStore.observableOfStream<Chat> chatStream |> fst 
let chatD = chatObs.Subscribe(fun (dt,v) -> printfn "%s: %s" v.sender v.message)
chatD.Dispose()

type ViewModel private () = 
    let propertyChanged = Event<System.ComponentModel.PropertyChangedEventHandler,System.ComponentModel.PropertyChangedEventArgs>()
    let valueChanged = Event<float>()
    let notify obj s = propertyChanged.Trigger(obj, System.ComponentModel.PropertyChangedEventArgs(s))
    let mutable value = 0.
    interface System.ComponentModel.INotifyPropertyChanged  with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish    
    
    member this.Value with get() = value and set(x) = value <- x; notify this "Value"; valueChanged.Trigger(x)
    member this.ValueChanged = valueChanged.Publish
    member this.SilentUpdate(x) = value <- x; notify this "Value";
    static member val Instance = ViewModel()

let trailing (timespan:TimeSpan) (obs:IObservable<'a>) =
        obs.Timestamp()
        |> Observable.scan (fun ys x ->  
                let now = DateTime.UtcNow
                x :: (ys |> List.filter (fun x -> (now - x.Timestamp.UtcDateTime) < timespan))) []
        |> Observable.map (fun xs -> [| for x in xs -> (x.Timestamp.UtcDateTime,x.Value) |])
           

Observable.Sample(ViewModel.Instance.ValueChanged, TimeSpan.FromSeconds(0.2))
|> Observable.add (fun x -> EventStore.postData stream x |> ignore)

(* Charting *)

type DateValue = {
    date : DateTime
    value : float
}

let f _ =
    let dp = DockPanel()
    let slider = Slider(Orientation = Orientation.Vertical, Minimum = 0., Maximum = 100.)
    slider.DataContext <- ViewModel.Instance
    slider.SetBinding(Slider.ValueProperty, Data.Binding("Value")) |> ignore
     
    
    let chart = XYChart()
    let xAxis = XYDateTimeAxis()
    let yAxis = XYDoubleAxis(Minimum = 0., Maximum = 100.)
    let lineSeries = LineSeries(LineKind = XYSeriesLineKind.Spline,
                                IsAggregationEnabled = false,
                                XPath = "date", XAxis = xAxis, YPath = "value", YAxis = yAxis)
    chart.Series.Add(lineSeries)
    chart.XAxes.Add(xAxis)
    chart.YAxes.Add(yAxis)

    Observable.Interval(TimeSpan.FromSeconds(0.2))
              .CombineLatest(obs,fun _ x -> x)
              .Sample(TimeSpan.FromSeconds(0.2))    
    |> trailing (TimeSpan.FromMinutes(1.))
    |> Observable.add (fun xs -> Dispatcher.invoke(fun _ -> lineSeries.ItemsSource <- [|for x in xs -> { date = fst x; value = snd (snd x)}|]))
    
    DockPanel.SetDock(slider,Dock.Left)
    DockPanel.SetDock(chart,Dock.Right)
    dp.Children.Add(slider) |> ignore
    dp.Children.Add(chart) |> ignore
    dp :> UIElement

Tsunami.IDE.SimpleUI.addControlToNewDocument("Event Stream", f)
Tsunami.IDE.SimpleUI.addControlToNewDocument("Event Stream", f)

