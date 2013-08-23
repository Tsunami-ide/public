#r "Tsunami.IDEDesktop.exe"
type H = Hive.HiveTypeProvider<DSN="Cloudera">

let hive = H.GetDataContext()

open Tsunami.TypeProvider.Hive

let xs = 
    hiveQuery {for x in hive.abalone do
                    where (x.sex <> "I")
                    select x.wholeweight
              } |> Seq.toArray 

open Tsunami.IDE.FSharp.Charting
#r "System.Drawing.dll"
open System


xs
|> Seq.countBy (fun x -> Math.Round(x.Value,1))
|> Seq.sortBy fst
|> Seq.map (fun (weight,count) -> (string weight, float count))
|> Chart.Column



let (X,y) = 
    let rows = hive.breastcancer |> Seq.toArray
    let X = rows |> Array.map (fun row -> [|row.col3;row.col4;row.col5;row.col6;row.col7;row.col8;row.col9;row.col10;row.col11;row.col12;row.col13;row.col14;row.col15;row.col16;row.col17;row.col18;row.col19;row.col20;row.col21;row.col22;row.col23;row.col24;row.col25;row.col26;row.col27;row.col28;row.col29;row.col30;row.col31;row.col32|])
    let y = rows |> Array.map (fun row -> if row.diagnosis = "M" then 1 else 0)
    (X,y)


#load @"..\Machine Learning\RandomForest.fsx"
#load @"..\Utilities\MachineLearning.fsx"

open Tsunami.Public.MachineLearning.RandomForest
open Tsunami.Public.Utilities.MachineLearing

let rand = Random(0)
let trainingSet,validationSet = [|0..X.Length-1|] |> Array.partition (fun _ -> rand.Next(5) <> 0)
let treeCount = 100
let forest = RandomForest(X,y,trainingSet,treeCount,true,rand)

let runWithBias x =
    let bias = [(0,1.);(1,x)] |> Map.ofSeq
    let predictions = validationSet |> Array.map (predictResult X forest (Some(bias)))
    let actuals = validationSet  |> Array.map (fun i -> y.[i])
    // Return false if 0
    let toBool (xs:int[]) = xs |> Array.map ((<>) 0)
    analyze (actuals |> toBool) (predictions |> toBool) 

let runs = 
    [|1..10|] 
        |> Array.map (fun i -> runWithBias (float i/3.)) 
        |> Array.map getSummary

Chart.Combine 
      ([|
            "True Positive", runs |> Array.map (fun x -> float x.truePositive)
            "False Positive", runs |> Array.map (fun x -> float x.falsePositive)
            "True Negative", runs |> Array.map (fun x -> float x.trueNegative)
            "False Negative", runs |> Array.map (fun x -> float x.falseNegative)
       |] |> Array.map (fun (name,values) -> Chart.Line(values |> Seq.mapi (fun x y -> (x,y)), Name = name)))
      
Chart.Combine 
     ([|
            "Precision", runs |> Array.map (fun x -> float x.precision)
            "Accuracy", runs |> Array.map (fun x -> float x.accuracy)
            "Recall", runs |> Array.map (fun x -> float x.recall)
      |] |> Array.map (fun (name,values) -> Chart.Line(values |> Seq.mapi (fun x y -> (x,y)), Name = name)))