#r "System.Core.dll"
#r "System.dll"
#r "System.Net.dll"
#r "System.Runtime.Serialization.dll"
#r "System.ServiceModel.Web.dll"
#r "System.Windows.Browser.dll"
#r "System.Windows.dll"
#r "System.Xml.dll"
#r "System.Runtime.Serialization.Json.dll"
#r "System.Windows.Controls.dll"
#r "System.Windows.Data.dll"
#r "System.ComponentModel.DataAnnotations.dll"
#r "System.Xml.Linq.dll"
#r "ActiproSoftware.BarCode.Silverlight.dll"
#r "ActiproSoftware.Charts.Silverlight.dll"
#r "ActiproSoftware.MicroCharts.Silverlight.dll"
#r "ActiproSoftware.Shared.Silverlight.dll"
#r "ActiproSoftware.SyntaxEditor.Addons.DotNet.Silverlight.dll"
#r "ActiproSoftware.SyntaxEditor.Addons.Xml.Silverlight.dll"
#r "ActiproSoftware.SyntaxEditor.Silverlight.dll"
#r "ActiproSoftware.Text.Addons.DotNet.Silverlight.dll"
#r "ActiproSoftware.Text.Addons.Xml.Silverlight.dll"
#r "ActiproSoftware.Text.LLParser.Silverlight.dll"
#r "ActiproSoftware.Text.Silverlight.dll"
#r "ActiproSoftware.Themes.Office.Silverlight.dll"
#r "ActiproSoftware.Views.Silverlight.dll"
#r "ActiproSoftware.Wizard.Silverlight.dll"
#r "Telerik.Windows.Controls.Data.dll"
#r "Telerik.Windows.Controls.DataServices.dll"
#r "Telerik.Windows.Controls.Docking.dll"
#r "Telerik.Windows.Controls.Input.dll"
#r "Telerik.Windows.Controls.Navigation.dll"
#r "Telerik.Windows.Controls.RibbonView.dll"
#r "Telerik.Windows.Controls.dll"
#r "Telerik.Windows.Data.dll"
#r "ActiproUtilities.dll"
#r "Crystalbyte.Net.dll"
#r "FSharp.Compiler.Silverlight.dll"
#r "Ionic.Zip.dll"
#r "NewtonSoft.Json.dll"
#r "System.Reactive.Core.dll"
#r "System.Reactive.Interfaces.dll"
#r "System.Reactive.Linq.dll"
#r "System.Reactive.PlatformServices.dll"
//#r "System.Threading.Tasks.SL5.dll"
#r "Tsunami.IDESilverlight.dll"


open System
open System.IO
open System.Net
open System.Text
open System.Collections.Generic
open Tsunami.Utilities
open Tsunami.SerDes.JS
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module EventStore =
    /// Should be private, ignore
    type Wrapped<'a> =
      {
        wrapped: 'a
      }

    /// Should be private, ignore
    type Event<'a> =
      {
        eventId: string
        eventType: string
        data: Wrapped<'a>
      }

    /// Should be private, ignore
    let mkEvent (ty: string) (data: 'a) =
      {
        eventId = Guid.NewGuid().ToString()
        eventType = ty
        data = { wrapped = data }
      }
    
    let private webClient = WebClient()
    
    /// Adds element 'e' to the stream identified by 'url'.
    /// It must be possible to serialize 'e' to JSON.
    let postData (url: string) (e : 'T) =
      let ev = mkEvent "null" e
      let j = toJSON [|ev|]
      let bs = Encoding.UTF8.GetBytes j

      let req = HttpWebRequest.Create(url)
      req.Method <-"POST"
      req.ContentType <- "application/json"  
      req.ContentLength <- int64 bs.Length
      let stream = Async.FromBeginEnd(req.BeginGetRequestStream, req.EndGetRequestStream) |> Async.RunSynchronously
      stream.Write(bs, 0, bs.Length)
      stream.Close()

      try
          req.AsyncGetResponse() |> Async.RunSynchronously
      with
      | :? System.Net.WebException as e -> e.Response


    let private get (hostUri:Uri) (url: string) : string =
      let uri = UriBuilder(url)
      uri.Port <- hostUri.Port
      uri.Host <- hostUri.Host
      let url2 = uri.Uri.ToString()
      //printfn "url: %s" url2
      let req = HttpWebRequest.CreateHttp(url2) 
      req.Method <- "GET"
      req.Accept <- "application/json"  
      //let stream = Async.FromBeginEnd(req.BeginGetRequestStream, req.EndGetRequestStream) |> Async.RunSynchronously
      //stream.Close() 
      let resp = Async.FromBeginEnd(req.BeginGetResponse, req.EndGetResponse) |> Async.RunSynchronously
      
      //let resp = req.AsyncGetResponse() |> Async.RunSynchronously
      let bytes = resp.GetResponseStream().ToBytes()
      
      let str = Encoding.UTF8.GetString(bytes,0,bytes.Length)
      
      //printfn "%O" uri.Uri
      //let str = webClient.AsyncDownloadString(uri.Uri) |> Async.RunSynchronously
      //printfn "%s" str
      str
      
      
    let private getUriByRelation (relation:string) (o: seq<JToken>) : string =
      Seq.pick (fun (o : JToken) -> if o.["relation"].ToString() = relation
                                    then Some (o.["uri"].ToString()) else None) o

    let private eventUrls (j: JObject) =
       j.["entries"].Children()
       |> Seq.map (fun o -> (o.["updated"].Value<DateTime>(), o.["links"]))
       |> Seq.map (fun (x,y) -> (x, y |> getUriByRelation "alternate"))

    /// Given a url of a stream, constructs a pair of objects, one being a
    /// "hot" observable stream and the other being a disposable used to stop
    /// polling of the underlying event store.
    let observableOfStream<'T> (url: string) : IObservable<DateTime*'T> * IDisposable =
      let hostUri = Uri(url)
      let cell =
        Agent.Start (fun inbox ->
            let observers = HashSet HashIdentity.Reference

            let rec loop url =
              async {
                let! command = inbox.TryReceive(250)
                match command with
                | Some (Choice1Of3 o) ->
                    observers.Add o |> ignore
                    return! loop url
                | Some (Choice2Of3 o) ->
                    observers.Remove o |> ignore
                    return! loop url
                | Some (Choice3Of3 o) -> ()

                | None ->
                  let j = (get hostUri url) |> JObject.Parse
                  if j.["entries"].HasValues then
                    for (datetime,url) in eventUrls j |> Seq.toArray |> Array.rev do
                      let value = (get hostUri url) |> Tsunami.SerDes.JS.fromJSON<Wrapped<'T>>
                      for (o: IObserver<DateTime*'T>) in observers do
                        o.OnNext(datetime,value.wrapped)
              
                    return! loop (j.["links"].Children() |> getUriByRelation "previous")
                  else
                    return! loop url
              }
            loop url
          )

      cell.Error.Add(fun exn ->
        printfn "Raised an exception: %A" exn)
  
      let o =
        { new IObservable<DateTime*'T> with
            member __.Subscribe observer = 
                      cell.Post(Choice1Of3 observer)
                      {   new IDisposable with
                          member __.Dispose() = cell.Post(Choice2Of3 observer) 
                      }
        }
      let d =
        {    
          new IDisposable with
              member __.Dispose() = cell.Post(Choice3Of3 ())
        }

      o,d

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

let stream = "http://127.0.0.1:4531/streams/floatstream"

EventStore.postData stream 4.0 |> ignore

let obs = EventStore.observableOfStream<float> stream |> fst 
let d = obs.Subscribe(fun (dt,v) -> printfn "Received %s %f" (dt.ToString()) v) 
d.Dispose()

let chatStream = "http://127.0.0.1:4531/streams/chatstream"

type Chat = {
    sender : string
    message : string
}
let chatObs = EventStore.observableOfStream<Chat> chatStream |> fst 
let chatD = chatObs.Subscribe(fun (dt,v) -> printfn "%s: %s" v.sender v.message)
EventStore.postData chatStream {sender = "John"; message = "Hello Matt"} |> ignore
chatD.Dispose()

let trailing (timespan:TimeSpan) (obs:IObservable<'a>) =
        obs.Timestamp()
        |> Observable.scan (fun ys x ->  
                let now = DateTime.UtcNow
                x :: (ys |> List.filter (fun x -> (now - x.Timestamp.UtcDateTime) < timespan))) []
        |> Observable.map (fun xs -> [| for x in xs -> (x.Timestamp.UtcDateTime,x.Value) |])
           



(* Charting *)
type DateValue = {
    date : DateTime
    value : float
}

let mutable lastValue = 0.
let mutable newValue = 0.
async {
    while true do
        do! Async.Sleep(500)
        if lastValue <> newValue then
            EventStore.postData stream newValue |> ignore 
            lastValue <- newValue
} |> Async.StartImmediate

let f _ =
    let dp = Grid()
    dp.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Pixels 30))
    dp.ColumnDefinitions.Add(ColumnDefinition())
    let slider = Slider(Orientation = Orientation.Vertical, Minimum = 0., Maximum = 100.)
    slider.ValueChanged.Add(fun x -> newValue <- x.NewValue)        
    
    let chart = XYChart()
    let xAxis = XYDateTimeAxis()
    let yAxis = XYDoubleAxis(Minimum = 0., Maximum = 100.)
    let lineSeries = LineSeries(LineKind = XYSeriesLineKind.Spline,
                                IsAggregationEnabled = false,
                                XPath = "date", XAxis = xAxis, YPath = "value", YAxis = yAxis)
    chart.Series.Add(lineSeries)
    chart.XAxes.Add(xAxis)
    chart.YAxes.Add(yAxis)

    let xs = Observable.Interval(TimeSpan.FromSeconds(0.2))
              .CombineLatest(obs,fun _ x -> x)
              .Sample(TimeSpan.FromSeconds(0.2))    
                |> trailing (TimeSpan.FromMinutes(1.))
    
    xs.Subscribe(fun xs -> Dispatcher.invoke(fun _ -> lineSeries.ItemsSource <- [|for x in xs -> { date = fst x; value = snd (snd x)}|])) |> ignore
    
    Grid.SetColumn(slider,0)
    Grid.SetColumn(chart,1)
    dp.Children.Add(slider) |> ignore
    dp.Children.Add(chart) |> ignore
    dp :> UIElement


open  Telerik.Windows.Controls

let addControlToNewDocument(name,f:unit -> UIElement) =
        Dispatcher.invoke(fun () ->
            // Find panes group
            let window = Application.Current.RootVisual :?> Tsunami.IDESilverlight.MainWindow
            let grid = window.Content :?> Grid
            let docking = grid.Children |> Seq.pick (function :? RadDocking as x -> Some x | _ -> None) 
            let container = docking.Items |> Seq.pick (function :? RadSplitContainer as x -> Some x | _ -> None)
            let group = container.Items |> Seq.pick (function :? RadPaneGroup as x -> Some x | _ -> None)
            // Add pane
            let pane = RadPane(Header=name)
            pane.MakeFloatingDockable()
            group.Items.Add(pane)
            // Set content
            pane.Content <- f())

addControlToNewDocument("Event Stream", f)        
