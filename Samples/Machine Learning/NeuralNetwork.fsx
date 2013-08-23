// http://retomatter.blogspot.ch/2012/12/functional-feed-forward-neural-networks.html
#load "MathNet.fsx"
open MathNet
open System
open MathNet.Numerics
open MathNet.Numerics.LinearAlgebra.Double

open MathNet.Numerics.Distributions

let m = matrix [ [1.0; 1.0]; [0.0; 1.0] ]
let v = vector [ 5.5; 3.2 ]
let res = m * v
type Matrix = MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>
type Vector = MathNet.Numerics.LinearAlgebra.Generic.Vector<float>

let prepend value (vec : Vector)  = vector [ yield! value :: (vec |> Vector.toList) ] 
let prependForBias : Vector -> Vector = prepend 1.0
let layer f (weights : Matrix) (inputs : Vector) =  (weights * inputs) |> Vector.map f |> prependForBias

/// the activation function
let sigmoid x = 1.0 / (1.0 + exp(-x))

// A BackProp Implementation in F#

/// combine weights and activation functions in a type
type NnetProperties = {
        Weights : Matrix list
        Activations : (float -> float) list
    }

/// compute the derivative of a function, midpoint rule
let derivative f = 
    /// precision for calculating the derivatives
    let prc = 1e-6
    fun x -> ((f (x + prc/2.0) - f (x - prc/2.0)) / prc)

/// returns list of (out, out') vectors per layer
let feedforward (netProps : NnetProperties) input = 
    List.fold 
        (fun (os : (Vector * Vector) list) (W, f) -> 
            let prevLayerOutput = 
                match os.IsEmpty with
                | true -> input
                | _    -> fst (os.Head)
            let prevOut = prependForBias prevLayerOutput 
            let layerInput = (W * prevOut)
            (layerInput  |> Vector.map f, 
             layerInput |> Vector.map (derivative f)) :: os) 
      [] (List.zip netProps.Weights netProps.Activations)

/// matlab like pointwise multiply
let (.*) (a : Vector) (b : Vector) = a.PointwiseMultiply(b)

/// computes the error signals per layer
/// starting at output layer towards first hidden layer
let inline errorSignals (Ws : Matrix list) layeroutputs target = 
    let trp = fun (W : Matrix) -> Some(W.Transpose())

    // need weights and layer outputs in reverse order, 
    // e.g starting from output layer
    let weightsAndOutputs = 
        let transposed = Ws |> List.tail |> List.map trp |> List.rev
        List.zip (None :: transposed) layeroutputs

    List.fold (fun (prevDs:Vector list) ((W : Matrix option), (o, o')) -> 
        match W with
        | None    -> (o' .* (target - o)) :: prevDs 
        | Some(W) -> let ds = prevDs.Head
                     (o' .* (W * ds).[1..] ) :: prevDs) 
      [] weightsAndOutputs

/// computes a list of gradients matrices
let inline gradients (Ws : Matrix list) layeroutputs input target = 
    let actualOuts = 
        layeroutputs |> List.unzip |> fst |> List.tail |> List.rev
    let signals = errorSignals Ws layeroutputs target
    (input :: actualOuts, signals) 
        ||> List.zip 
        |> List.map (fun (zs, ds) -> 
            ds.OuterProduct(prependForBias zs))

let eta = 0.8
let alpha = 0.25

/// updates the weights matrices with the given deltas 
/// of timesteps (t) and (t-1)
/// returns the new weights matrices
let updateWeights Ws (Gs : Matrix list) (prevDs : Matrix list) = 
    (List.zip3 Ws Gs prevDs) 
        |> List.map (fun (W, G, prevD) ->
            let dW = eta * G + (alpha * prevD)
            W + dW, dW)

/// for each weight matrix builds another matrix with same dimension
/// initialized with 0.0
let initDeltaWeights (Ws : Matrix list) = 
    Ws |> List.map (fun W -> DenseMatrix.Create (W.RowCount, W.ColumnCount, fun _ _ -> 0.0) :> Matrix ) 


let step netProps prevDs input target = 
    let layeroutputs = feedforward netProps input
    let Gs = gradients netProps.Weights layeroutputs input target
    (updateWeights netProps.Weights Gs prevDs)

// MATT: Note - change to use mini-batches instead of single sample
let train netProps samples epoches = 
    let count = samples |> Array.length
    let rnd = Random()
    let Ws, fs = netProps.Weights, netProps.Activations
    let rec loop Ws Ds i =
        if i < (epoches * count) 
        then
            let inp, trg = samples.[rnd.Next(count)]
            let netProps = { Weights = Ws; Activations = fs }
            let ws, ds = List.unzip (step netProps Ds inp trg)
            loop ws ds (i + 1)
        else Ws
    let Ws' = loop Ws (initDeltaWeights Ws) 0
    { netProps with Weights = Ws' }

/// returns the output vector from a given list of layer outputs
let netoutput (layeroutputs : ('a * 'a) list) = 
    fst (layeroutputs.Head)

/// computes the output error from a
/// given target and an actual output vector
let error (target : Vector) (output : Vector) =
    ((target - output) 
        |> Vector.map (fun x -> x * x) 
        |> Vector.toArray 
        |> Array.sum) / 2.0


let initWeights rows cols f = DenseMatrix.OfArray(Array2D.init rows cols (fun _ _ -> f())) 
        
let targetFun = fun x -> sin (6.0 * x)

let computeResults netProps trainingset epoches = 
    let netProps' = train netProps trainingset epoches
    let setSize = trainingset.Length

    let error = 
        trainingset 
        |> Array.fold (fun E (x, t) -> 
            let outs = feedforward netProps' x
            let En = error t (netoutput outs)
            E + En) 0.0

    let outputs = 
        [-0.5 .. 0.01 .. 0.5]
        |> List.fold (fun outs x -> 
            let layeroutputs = 
                feedforward netProps' (vector [x])
            let o = (netoutput layeroutputs).At 0
            (x,o) :: outs) []
        |> List.rev

    (error / (float setSize), outputs)

let experimentSetting() = 
    let rnd = Random()
    let randZ() = rnd.NextDouble() - 0.5

    let samples = 
        [| for i in 1 .. 25 -> randZ() |] 
        |> Array.map(fun x -> 
            x, targetFun(x) + 0.15 * randZ())

    let trainingSet = 
        samples 
        |> Array.map (fun (x,y) -> vector [x], vector [y])
    
    let Wih = initWeights 15 2 randZ
    let Who = initWeights 1 16 randZ
    let netProps = { Weights = [Wih; Who]; 
                     Activations = [sigmoid; tanh]}
    (samples, trainingSet, netProps)

let testRun experiment listOfEpoches =
    let samples, ts, netProps = experiment()
    let data = 
        listOfEpoches
        |> List.fold (fun acc N ->
            let error, outs = computeResults netProps ts N
            printfn "mean error after %d epoches: %f" N error
            outs :: acc) []
        |> List.rev
    
    (samples, data)

let samples, data = testRun experimentSetting [75; 500; 2000; 4000];;