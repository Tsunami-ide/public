module Tsunami.Public.MathNet

#r @"nuget://MathNet.Numerics/2.5.0/lib/net40/MathNet.Numerics.dll"
#r @"nuget://MathNet.Numerics.FSharp/2.5.0/lib/net40/MathNet.Numerics.FSharp.dll"

open System
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra.Double
open MathNet.Numerics.Statistics
open MathNet.Numerics.Distributions

// http://fssnip.net/raw/dQ
type MathNet.Numerics.LinearAlgebra.Generic.
    Vector<'T when 'T : struct and 'T : (new : unit -> 'T) 
               and 'T :> System.IEquatable<'T> and 'T :> System.IFormattable 
               and 'T :> System.ValueType> with
  /// Implements slicing of vector - both arguments are option types
  member x.GetSlice(start, finish) = 
    let start = defaultArg start 0
    let finish = defaultArg finish (x.Count - 1)
    x.SubVector(start, finish - start + 1)

//
//type MathNet.Numerics.LinearAlgebra.Double.Vector with
//  /// Implements slicing of vector - both arguments are option types
//  member x.GetSlice(start, finish) = 
//    let start = defaultArg start 0
//    let finish = defaultArg finish (x.Count - 1)
//    x.SubVector(start, finish - start + 1) :?> Vector


// Define type extension for the generic matrix type
// http://fssnip.net/raw/dQ
type MathNet.Numerics.LinearAlgebra.Generic.
    Matrix<'T when 'T : struct and 'T : (new : unit -> 'T) 
               and 'T :> System.IEquatable<'T> and 'T :> System.IFormattable 
               and 'T :> System.ValueType> with
  // Implement slicing for matrices (using rows & columns)
  member x.GetSlice(rstart, rfinish, cstart, cfinish) = 
    let cstart = defaultArg cstart 0
    let rstart = defaultArg rstart 0
    let cfinish = defaultArg cfinish (x.ColumnCount - 1)
    let rfinish = defaultArg rfinish (x.RowCount - 1)
    x.SubMatrix(rstart, rfinish - rstart + 1, cstart, cfinish - cstart + 1)

//type MathNet.Numerics.LinearAlgebra.Double.Matrix with
//  // Implement slicing for matrices (using rows & columns)
//  member x.GetSlice(rstart, rfinish, cstart, cfinish) = 
//    let cstart = defaultArg cstart 0
//    let rstart = defaultArg rstart 0
//    let cfinish = defaultArg cfinish (x.ColumnCount - 1)
//    let rfinish = defaultArg rfinish (x.RowCount - 1)
//    x.SubMatrix(rstart, rfinish - rstart + 1, cstart, cfinish - cstart + 1) :?> Matrix

type Matrix = MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>
type Vector = MathNet.Numerics.LinearAlgebra.Generic.Vector<float>

[<CompilationRepresentationAttribute(CompilationRepresentationFlags.ModuleSuffix)>]
module Vector =
    let rep (v: Vector) (n: int) : Matrix =
        DenseMatrix.initRow n v.Count (fun _ -> v) :> Matrix

    let ones n =
        DenseVector.create n 1.0 :> Vector

[<CompilationRepresentationAttribute(CompilationRepresentationFlags.ModuleSuffix)>]
module Matrix =

    /// Returns a normalised version of the matrix
    /// along with the mean and standard deviation
    let featureNormalize (X: Matrix) =
        let mu =
            X.ColumnEnumerator()
            |> Seq.map (fun (_, col) -> col.Mean())
            |> DenseVector.ofSeq

        let sigma =
            X.ColumnEnumerator()
            |> Seq.map (fun (_, col) -> col.StandardDeviation())
            |> DenseVector.ofSeq

        let X' = Matrix.mapCols (fun i col -> (col - mu.[i]) / sigma.[i]) X

        (X', mu, sigma)

    let exp X = X |> Matrix.map (fun x -> Math.Exp(x))
    let log X = X |> Matrix.map (fun x -> Math.Log(x))

    let zeros m n = new DenseMatrix(m,n) :> Matrix
    let ofSeqRows = DenseMatrix.ofSeq
    let ofSeqCols ss = DenseMatrix.ofColumns (Seq.length (Seq.head ss)) (Seq.length ss) ss

    let sigmoid X =
        Matrix.map (fun z -> 1.0 / (1.0 + Math.Pow(MathNet.Numerics.Constants.E, - z))) X

let inline (.*) (a:^T) (b:^T) = 
  (^T : (member PointwiseMultiply : ^T -> ^R) (a, b))

let (.+) (X: Matrix) (n: float) = Matrix.map ((+) n) X
let (./) (X: Matrix) (n: float) = Matrix.map (fun x -> x / n) X
let (.^) (X: Vector) (n: float) = Vector.map (fun x -> Math.Pow(x,n)) X
let tr (X: Matrix) = X.Transpose()
