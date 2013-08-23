#load "..\..\Utilities\WebData.fsx"
#load "..\RandomForest.fsx"
#load "..\..\Utilities\MachineLearning.fsx"
open System
open System.IO
open Tsunami.Public
open Tsunami.Public.MachineLearning
open Tsunami.Public.Utilities.MachineLearing
open RandomForest


let extractXy (file:string) =
    let lines = WebData.ReadAllLines(sprintf @"http://tsunami.io/data/%s" file)
    lines.[1..]
    |> Array.map (fun x -> x.Split([|','|]))
    |> Array.map (fun xs -> (xs.[1..] |> Array.map float, int xs.[0]))
    |> Array.unzip

let (trainX,trainY) = extractXy (@"digitssample.csv")
let (validateX,validateY) = extractXy (@"digitscheck.csv")

let forest = SimpleRandomForest(trainX,trainY,100)
let predictions = [|0 .. validateY.Length - 1|] |> Array.map (predictResult validateX forest None)
let actuals = validateY
let results = confusionMatrix predictions actuals 

results |> printConfusionMatrix
results |> confusionMatrixAccuracy