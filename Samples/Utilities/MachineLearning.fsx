module Tsunami.Public.Utilities.MachineLearing

type Summary = {
    truePositive : int
    falsePositive : int
    trueNegative : int

    falseNegative : int
    ///tp/(tp+fp)
    precision : float
    ///tp/(tp+fn)
    recall : float
    ///(tp + tn)/all
    accuracy : float
}

let summarize(tp,fp,fn,tn) =
        printfn "True Positive: \t\t%i\nFalse Positive: \t%i\nFalse Negative: \t%i\nTrue Negative: \t\t%i" tp fp fn tn
        printfn "Precision tp/(tp+fp): \t\t%f" (float tp / float (tp + fp))
        printfn "Recall (tp/(tp+fn): \t\t%f" (float tp / float (tp + fn))
        printfn "True Negative (tn/(tn+fp): \t%f" (float tn / float (tn + fn))
        printfn "Accuracy (tp + tn)/all: \t%f" (float (tp + tn) / float (tp + fp + tn + fn))
        printfn ""

let getSummary(tp,fp,fn,tn) = 
    {
        truePositive = tp
        falsePositive = fp
        trueNegative = tn
        falseNegative = fn
        precision = (float tp / float (tp + fp))
        recall = (float tp / float (tp + fn))
        accuracy = (float (tp + tn) / float (tp + fp + tn + fn))
    }

let analyze (actual:bool[]) (predicted:bool[]) =
    let count xs = xs |> Seq.sumBy (function |true -> 1 | false -> 0)
    let tp = (predicted,actual) ||> Array.map2 (&&) |> count
    let fp = (predicted,actual) ||> Array.map2 (fun x y -> x && not y) |> count
    let fn = (predicted,actual) ||> Array.map2 (fun x y -> (not x) && y) |> count
    let tn = (predicted,actual) ||> Array.map2 (fun x y -> (not x) && (not y)) |> count
    (tp,fp,fn,tn)

let confusionMatrix (actual:int[]) (predicted:int[]) =
    (actual,predicted) 
    ||> Array.zip 
    |> Seq.countBy (fun x -> x)
    |> Map.ofSeq

let printConfusionMatrix (map:Map<(int*int),int>) =
    let xs = map |> Seq.collect (fun x -> [fst x.Key; snd x.Key]) |> Seq.distinct |> Seq.sort
    for x in xs do printf "\t%i" x
    printfn ""
    for x in xs do 
        printf "%i" x
        for x2 in xs do
            printf "\t%i" (match map.TryFind((x,x2)) with | Some(x) -> x | _ -> 0)
        printfn ""
    printfn ""

let confusionMatrixAccuracy (map:Map<(int*int),int>) =
    let xs = map |> Seq.collect (fun x -> [fst x.Key; snd x.Key]) |> Seq.distinct |> Seq.sort
    float ([for x in xs -> match map.TryFind((x,x)) with | Some(value) -> value | None -> 0] |> Seq.sum) / float (map |> Seq.map (fun x -> x.Value) |> Seq.sum)
