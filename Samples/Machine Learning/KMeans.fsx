module Tsunami.Public.MachineLearning.KMeans
#load "..\Utilities\Utilities.fsx"
open Tsunami.Public.Utilities

/// Group all the vectors by the nearest center. 
let classify centers vectors = 
    vectors |> Array.groupBy (fun v -> centers |> Array.minBy (Array.distance v))

/// Repeatedly classify the vectors, starting with the seed centers, taking the n'th result
let computeCentroids seed vectors n = 
    seed |> Seq.iterate (fun centers -> classify centers vectors |> Array.map Array.average) |> Seq.skip n |> Seq.head