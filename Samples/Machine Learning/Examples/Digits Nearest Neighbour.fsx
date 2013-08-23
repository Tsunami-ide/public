#load "..\..\Utilities\WebData.fsx"
#load "..\..\Utilities\Utilities.fsx"
#load "..\..\Utilities\MachineLearning.fsx"
#load "..\KNearestNeighbours.fsx"

open System
open System.IO
open Tsunami.Public
open Tsunami.Public.Utilities
open Tsunami.Public.Utilities.MachineLearing
open Tsunami.Public.MachineLearning.KNearestNeighbours

let extractXy (file:string) =
    let lines = WebData.ReadAllLines(sprintf @"http://tsunami.io/data/%s" file)
    lines.[1..]
    |> Array.map (fun x -> x.Split([|','|]))
    |> Array.map (fun xs -> (xs.[1..] |> Array.map float, int xs.[0]))
    |> Array.unzip

let (trainX,trainY) = extractXy (@"digitssample.csv")
let (validateX,validateY) = extractXy (@"digitscheck.csv")


let predictions = validateX |> Array.map (fun row -> findNearestNeighbours trainX row |> Array.map (fun (i,_) -> trainY.[i]) |> Array.mode)

let actuals = validateY    
    
let results = confusionMatrix predictions actuals 
results |> printConfusionMatrix
results |> confusionMatrixAccuracy

