module Tsunami.Public.MachineLearning.KNearestNeighbours
#load "..\Utilities\Utilities.fsx"
open Tsunami.Public.Utilities
let findNearestNeighbours (X:float[][]) (qs:float[])  =
    [|0..X.Length-1|]
    |> Array.fold (fun (acc:(int*float)[]) (i:int) -> 
        let dist = Array.distance qs X.[i]
        [|yield (i,dist); yield! acc|]  
        |> Array.sortBy snd 
        |> Array.truncate 10) [||]