module StockScript
/// A translation by Matthew Moloney of http://vstostocks.codeplex.com/ by Mathias Brandewinder (Twitter: @brandewinder Blog: http://www.clear-lines.com/blog/)
#r @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Visual Studio Tools for Office\PIA\Office14\Microsoft.Office.Interop.Excel.dll"
open Microsoft.Office.Interop.Excel
open System
open System.Net

type TradingDaySummary = {Day : DateTime; Volume : int64; Open : float; Close : float; High : float; Low : float}

type StockHistory = {Symbol : string; StartDate : DateTime; EndDate : DateTime; History : TradingDaySummary[]}

let read(symbol : string, startDate : DateTime, endDate : DateTime) : StockHistory =
    let query = sprintf "http://ichart.finance.yahoo.com/table.csv?s=%s&a=%i&b=%i&c=%i&d=%i&e=%i&f=%i&g=d&ignore=.csv" 
                    symbol (startDate.Month - 1) startDate.Day startDate.Year (endDate.Month - 1) endDate.Day endDate.Year
    
    use client = new WebClient()
    let history =
        let res = client.DownloadString(query)
        [|
            for line in res.Split([|char 10|]) |> Seq.skip 1 do
                 match line.Split([|','|]) with 
                        | [|day;``open``;high;low;close;volume; _|] -> 
                            yield {Day = DateTime.Parse(day); Open = float ``open``; High = float high; Low = float low; 
                                Close = float close; Volume = Int64.Parse volume}
                        | _ -> ()
        |]
    {Symbol = symbol; StartDate = startDate; EndDate = endDate; History = history}

let run (history : StockHistory, horizon : int, runs : int) =
    let closeSeries = history.History |> Array.map (fun x -> x.Close)
    let returnsSeries = closeSeries |> Seq.pairwise |> Seq.map (fun (today,tomorrow) -> (tomorrow - today) / today) |> Seq.toArray
    let seed = new Random()
    let initialValue = 100.
    [|0..runs|] 
    |> Array.map (fun _ -> 
        let rng = Random(seed.Next())
        /// Simulate
        async { 
            return
                [| 0..horizon - 1 |]
                |> Array.map (fun _ -> returnsSeries.[rng.Next(returnsSeries.Length)]) 
                |> Array.fold (fun value ret -> value * (1. + ret)) initialValue  
        })
    |> Async.Parallel
    |> Async.RunSynchronously    




type SeriesData = {Name : string; XValues : obj[]; Values : obj[] }
    
let appendSeries (chartType : XlChartType, seriesData : SeriesData) (chart : Chart) =
    chart.ChartType <- chartType
    let seriesCollection = chart.SeriesCollection() :?> SeriesCollection
    let series = seriesCollection.NewSeries()
    series.Name <- seriesData.Name
    series.Values <- seriesData.Values
    series.XValues <- seriesData.XValues
    chart
    
let addStockHistory (history : StockHistory) (chart : Chart) =
    let dataPoints = history.History |> Array.sortBy (fun x -> x.Day)
    let xValues = dataPoints |> Array.map (fun x -> box x.Day); 

    let close = {
                    Name = history.Symbol; 
                    XValues = xValues
                    Values = dataPoints |> Array.map (fun x -> box x.Close)
                }
    appendSeries (XlChartType.xlLine, close) chart |> ignore

let addMovingAverages (history : StockHistory) (chart : Chart) =
    let dataPoints = history.History |> Array.sortBy (fun x -> x.Day)
    let xValues = dataPoints |> Array.map (fun x -> box x.Day);
    let movingAverage (day:DateTime) (length:int) =
        let xs =
            dataPoints
            |> Seq.takeWhile (fun x -> x.Day <= day)
            |> Seq.toArray
            |> Array.rev
            |> Array.map (fun x -> x.Close)
            |> Seq.truncate length
            |> Seq.toArray
        if xs.Length = 0 then None else Some((xs |> Array.sum) / float xs.Length)


    for averages in [|10;50;100|] do
        let series = {
                Name = sprintf "MA %i days" averages
                XValues = xValues
                Values = dataPoints |> Array.choose (fun x -> movingAverage x.Day averages |> Option.map box)
            }
        appendSeries (XlChartType.xlLine, series) chart |> ignore
        


let addValueDistibution (history : StockHistory, horizon : int, runs : int) (chart : Chart) =
    let projections = run (history, horizon, runs)
    let min = projections |> Seq.min |> int
    let max = projections |> Seq.max |> int
    let xValues = [|min..max|]
    let values = [|for xValue in xValues -> projections |> Array.filter ((int >> (=) xValue)) |> Array.length |]
    let data = 
        {
            Name = history.Symbol
            XValues = xValues |> Array.map box
            Values = values |> Array.map box
        }
    appendSeries (XlChartType.xlColumnStacked, data) chart


let writeHistory(history : StockHistory, worksheet : Worksheet) =
    let DateColumn = 0
    let OpenColumn = 1
    let CloseColumn = 2

    let length = history.History.Length
    let dataArray = Array2D.init (length + 1) 6 (fun _ _ -> box null)
    let formatArray = Array2D.init (length + 1) 6 (fun _ _ -> box null)
    
    dataArray.[0,DateColumn] <- box "Date"
    dataArray.[0,OpenColumn] <- box "Open"
    dataArray.[0,CloseColumn] <- box "Close"

    history.History |> Array.iteri (fun i day -> 
        let line = i + 1
        
        dataArray.[line, DateColumn] <- box day.Day
        dataArray.[line, OpenColumn] <- box day.Open
        dataArray.[line, CloseColumn] <- box day.Close

        formatArray.[line, DateColumn] <- box "YYYY/MM/DD"
        formatArray.[line, OpenColumn] <- box "#,##0.00"
        formatArray.[line, CloseColumn] <- box "#,##0.00"
        )
    let range = worksheet.Range(worksheet.Cells.[1,1], worksheet.Cells.[length + 1, 6])
    range.Value2 <- dataArray
    range.NumberFormat <- formatArray

let clear (c:Chart) =
    let sc = c.SeriesCollection() :?> Microsoft.Office.Interop.Excel.SeriesCollection
    for i in [sc.Count .. -1 .. 1] do sc.Item(i).Delete() |> ignore