#r "System.Data"
#r "System.Core"
#r "System.Data.Linq.dll"
#r "System.Data.Entity.dll"
#r "cache:http://tsunami.io/assemblies/Microsoft.Office.Interop.Excel.dll"
#r "cache:http://tsunami.io/assemblies/office.dll"
#r "cache:http://tsunami.io/assemblies/FSharp.Data.TypeProviders.dll" 

#r "cache:http://tsunami.io/assemblies/FCell.XlProvider.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.dll"
#r "cache:http://tsunami.io/assemblies/MathNet.Numerics.FSharp.dll"

open System
open System.IO
open System.Data
open System.Data.Linq
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Data.TypeProviders
open MathNet.Numerics.Random
open MathNet.Numerics.Distributions
open FCell.XlProvider 
open FCell.TypeProviders.XlProvider
open Microsoft.Office.Interop.Excel

type dbSchema = SqlEntityConnection<"Server=tcp:xxxx.database.windows.net,1433;Database=TsunamiAzure;User ID=xxxx@xxxxx;Password=xxxx;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;">
let dc = dbSchema.GetDataContext()

let widgetReport = new XlWorkbook< "">()

let clearSQLAzure() =
    for x in dc.EUDoDLift do dc.EUDoDLift.DeleteObject(x)
    for x in dc.EUMoMLift do dc.EUMoMLift.DeleteObject(x)
    dc.DataContext.SaveChanges() 

let uploadExcelToAzure() =
    clearSQLAzure() |> ignore

    widgetReport.EUMoM
    |> Seq.iteri (fun i x -> dc.EUMoMLift.AddObject(dbSchema.ServiceTypes.EUMoMLift(ID = i, Lift = x)))

    widgetReport.EUDoD
    |> Seq.iteri (fun i x -> dc.EUDoDLift.AddObject(dbSchema.ServiceTypes.EUDoDLift(ID = i, Lift = x)))
    
    widgetReport.USAMoM 
    |> Seq.iteri (fun i x -> dc.USAMoMLift.AddObject(dbSchema.ServiceTypes.USAMoMLift(ID = i, Lift = x)))

    widgetReport.USADoD
    |> Seq.iteri (fun i x -> dc.USADoDLift.AddObject(dbSchema.ServiceTypes.USADoDLift(ID = i, Lift = x)))
    
    dc.DataContext.SaveChanges()
    
let downloadAzureToExcel() =
    widgetReport.EUDoD <- [| for x in  dc.EUDoDLift -> x |] |> Array.sortBy (fun x -> x.ID) |> Array.map (fun x -> x.Lift)
    widgetReport.EUMoM <- [| for x in  dc.EUMoMLift -> x |] |> Array.sortBy (fun x -> x.ID) |> Array.map (fun x -> x.Lift)
    widgetReport.USADoD <- [| for x in  dc.USADoDLift -> x |] |> Array.sortBy (fun x -> x.ID) |> Array.map (fun x -> x.Lift)
    widgetReport.USAMoM <- [| for x in  dc.USAMoMLift -> x |] |> Array.sortBy (fun x -> x.ID) |> Array.map (fun x -> x.Lift)
    
let clearExcel() =
    widgetReport.EUDoD <- Array.create 7 0.
    widgetReport.EUMoM <- Array.create 12 0.
    widgetReport.USADoD <- Array.create 7 0.
    widgetReport.USAMoM <- Array.create 12 0.

let genSynthetic() = 
    widgetReport.EUDoD <- Normal.WithMeanVariance(0.02, 0.003).Samples() |> Seq.take 7 |> Seq.toArray
    widgetReport.EUMoM <- Normal.WithMeanVariance(0.02, 0.001).Samples() |> Seq.take 12 |> Seq.toArray
    widgetReport.USADoD <- Normal.WithMeanVariance(0.05, 0.003).Samples() |> Seq.take 7 |> Seq.toArray
    widgetReport.USAMoM <- Normal.WithMeanVariance(0.05, 0.001).Samples() |> Seq.take 12 |> Seq.toArray

let demo() =
    async {
        clearExcel()
        genSynthetic()
        uploadExcelToAzure() |> ignore
        do! Async.Sleep 500
        clearExcel()
        do! Async.Sleep 500
        downloadAzureToExcel()
        do! Async.Sleep 500
        genSynthetic()
        do! Async.Sleep 500
        downloadAzureToExcel()
        clearExcel()
    } |> Async.RunSynchronously