#load @"C:\ExcelEnv.fsx"
#load @"C:\StocksScript.fsx"
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
    chart |> addStockHistory (read (stock, now.AddMonths(-6), now))
    chart |> addMovingAverages (read (stock, now.AddMonths(-6), now))
