//#load @"ExcelEnv.fsx"
#load "StocksScript.fsx"
open StockScript
open System
open System.Net
open Microsoft.Office.Interop.Excel


let now = System.DateTime.Now

let chart = Excel.NewChart().Value    

let msft = read ("MSFT", now.AddMonths(-6), now)
chart |> addStockHistory msft
chart |> addMovingAverages msft

chart |> clear 

for stock in ["AAPL";"MSFT";"GOOG"] do 
    let data = (read (stock, now.AddMonths(-6), now))
    chart |> addStockHistory data
    chart |> addMovingAverages data
