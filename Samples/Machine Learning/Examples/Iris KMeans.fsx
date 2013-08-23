#load @"..\KMeans.fsx"
#load "..\..\Utilities\WebData.fsx"
open System
open System.IO
open Tsunami.Public
open Tsunami.Public.Utilities
open Tsunami.Public.MachineLearning

let inputData =  WebData.ReadAllLines(@"http://tsunami.io/data/Iris.csv") |> Array.map (fun x -> x.Trim().Split(',') |> Array.take 4 |> Array.map float)
    
let initialCenters = inputData |> Array.take 3 
let finalCenters = KMeans.computeCentroids initialCenters inputData 5
let results = KMeans.classify finalCenters inputData