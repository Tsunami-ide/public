module Tsunami.Public.MachineLearning.RandomForest

open System
open System.Collections.Generic

let meanSdCount (xs:float[]) =
    match xs with
    | [||] -> failwith "requires at least one element"
    | [|x|] -> (x,0.,1)
    | xs ->
        let (newM, newS, n) =
            xs |> Seq.skip 1 |> Seq.fold(fun (oldM, oldS,(n:int)) x ->
                let newM = oldM + (x - oldM) / (float) n
                let newS = oldS + (x - oldM) * (x - newM)
                (newM,newS, n + 1)) (xs.[0],0.,2)
        (newM, Math.Sqrt(newS / (float) (n - 2)), n - 1)

type DecisionChoice = 
    { Indicator : int
      Threshold : float
      LessThan  : DecisionTree
      MoreThan  : DecisionTree }

and DecisionTree = 
    | DecisionChoice of DecisionChoice
    | DecisionResult of int

let chooseIndicator(X:float[][],y:int[],indicators:int[],rows:int[]) = 
    let results = rows |> Array.map (fun row -> y.[row])
    let split,indicator =
          indicators |> Array.map (fun i ->
              let (setMean,setSd,setCount) = rows |> Array.map (fun row -> X.[row].[i]) |> meanSdCount
              if setSd < Double.Epsilon 
              then
                  (-1.,(setMean,i))
              else
                  // group by category
                  let categoryStats =
                          rows 
                              |> Seq.groupBy (fun row -> y.[row])
                              |> Seq.map (fun (c,xs) -> xs |> Seq.toArray |> Array.map (fun x -> X.[x].[i]) |> meanSdCount)
                              |> Seq.toArray

                  // Split by weighted mean
                  let split = 
                      let (means,weights) = categoryStats |> Array.map (fun (mean,sd,_) -> (mean,setSd/(setSd + sd))) |> Array.unzip
                      ((means,weights) ||> Array.map2 (fun x y -> x * y) |> Array.sum) / (weights |> Array.sum)

                  let (_,sd,_) = categoryStats |> Array.map (fun (mean,_,_) -> mean) |> meanSdCount
                  (sd / setSd,(split,i)))    
                  |> Array.maxBy fst
                  |> snd
    (indicator,split)
        
let rec buildDecisionTree(X:float[][],y:int[],weights:Map<int,float>,indicators:int[],rows:int[],filterFeature:bool)  =
      let results = [ for row in rows do yield y.[row]]
      let homogenous = results |> Seq.pairwise |> Seq.exists (fun (x,y) -> x<>y) |> not
      let leafNode() =
            let category = 
                if rows.Length = 1 then y.[rows.[0]]
                else
                    results 
                        |> Seq.groupBy (fun x -> x) 
                        |> Seq.map (fun (x,xs) -> (x,xs |> Seq.length)) 
                        |> Seq.sortBy (fun (x,y) -> - float y * float weights.[x])
                        |> Seq.head 
                        |> fst
            DecisionResult category
      // Single Category?
      if indicators.Length <= 1 || rows.Length <= 1 || homogenous then 
          leafNode()
      else 
          let (indicator, threshold) = chooseIndicator(X,y,indicators,rows)
          let lessRows = [| for row in rows do if X.[row].[indicator] < threshold then yield row |]
          let moreRows = [| for row in rows do if X.[row].[indicator] >= threshold then yield row |]
          if (lessRows.Length = 0 || moreRows.Length = 0) then
              leafNode()
          else
              let indicators' = if filterFeature then indicators |> Array.filter ((<>) indicator) else indicators
              DecisionChoice
                  { Indicator = indicator
                    Threshold = threshold
                    LessThan = buildDecisionTree(X,y,weights,indicators',lessRows,filterFeature)
                    MoreThan = buildDecisionTree(X,y,weights,indicators',moreRows,filterFeature) }
        
let rec runDecisionTree(X:float[][]) (tree:DecisionTree) (row:int) = 
    match tree with 
    | DecisionResult r -> r
    | DecisionChoice dc ->
        if X.[row].[dc.Indicator] < dc.Threshold 
        then runDecisionTree X dc.LessThan row 
        else runDecisionTree X dc.MoreThan row

let buildRandomForest(X:float[][],y:int[],weights:Map<int,float>,indicators:int[],train:int[],treeCount:int,featureSampleCount:int,rand:Random,filterFeature:bool) =   
    [| for _ in 0..treeCount-1 do
        let indicators = [|for _ in 1..featureSampleCount do yield indicators.[rand.Next(indicators.Length)]|]
        let train,validate = train |> Array.partition (fun _ -> rand.Next(3) <> 0)
        yield buildDecisionTree(X,y,weights,indicators,train,filterFeature) |] 



let RandomForest(X:float[][],y:int[],train:int[],treeCount:int,resampleFeatures:bool,rand:Random) =   
    let featureCount = X |> Seq.head |> Seq.length
    let weights = y |> Seq.distinct |> Seq.map (fun i -> (i,1.)) |> Map.ofSeq // ignore weights
    let featureSampleCount = int (Math.Sqrt(featureCount |> float)) + 1
    let indicators = [|0..featureCount-1|]
    let filterFeatures = true
    buildRandomForest(X,y,weights, indicators,train,treeCount,featureSampleCount,rand,resampleFeatures)


let SimpleRandomForest(X:float[][],y:int[],treeCount:int) = RandomForest(X,y,[|0..y.Length-1|],treeCount,true,Random())   

let predictResult (X:float[][]) (trees:DecisionTree[]) (bias:Map<int,float> option) (v:int) : int =
    [| for tree in trees -> runDecisionTree X tree v |] 
    |> Seq.countBy (fun x -> x) 
    |> Seq.maxBy (fun (catagory,count) -> (match bias with | Some(x) -> x.[catagory] | _ -> 1.) * float count)
    |> fst


let topFeatures (trees:DecisionTree[]) =
    trees |> Array.choose (function | DecisionChoice(d) -> d.Indicator |> Some | _ -> None) 
          |> Seq.countBy (fun x -> x)
          |> Seq.sortBy snd
          |> Seq.toArray
          |> Array.rev
