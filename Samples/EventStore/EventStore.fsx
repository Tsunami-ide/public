module Tsunami.EventStore
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

open System
open System.IO
open System.Net
open System.Text
open System.Collections.Generic
open Tsunami.Utilities
open Tsunami.SerDes.JS
open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

/// Adds element 'e' to the stream identified by 'url'.
/// It must be possible to serialize 'e' to JSON.
let postData (url: string) (e : 'T) =
  let ev = mkEvent "null" e
  let j = toJSON [|ev|]
  let bs = Encoding.ASCII.GetBytes j

  let req = HttpWebRequest.Create(url)
  req.Method <- WebRequestMethods.Http.Post
  req.ContentType <- "application/json"  
  req.ContentLength <- bs.LongLength
  let stream = req.GetRequestStream()
  stream.Write(bs, 0, bs.Length)
  stream.Close()

  try
      req.GetResponse()
  with
  | :? System.Net.WebException as e -> e.Response


let private get (url: string) : string =
  let req = HttpWebRequest.Create(url) :?> HttpWebRequest
  req.Accept <- "application/json"  
  req.GetResponse().GetResponseStream().ToBytes()
  |> Encoding.ASCII.GetString

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
              let j = get url |> JObject.Parse
              if j.["entries"].HasValues then
                for (datetime,url) in eventUrls j |> Seq.toArray |> Array.rev do
                  let value = 
                        let str = get url
                        //printfn "uri: %O\njson: \n%s"  url str
                        str |> Tsunami.SerDes.JS.fromJSON<Wrapped<'T>>
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

