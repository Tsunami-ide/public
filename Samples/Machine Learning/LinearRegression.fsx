[<AutoOpen>]
module LinearRegression

#load @"../Utilities/Utilities.fsx"
#load @"MathNet.fsx"

open MathNet.Numerics.LinearAlgebra.Double
open Tsunami.Public.MathNet
open Tsunami.Public.Utilities

module LinearRegression =
    
    /// Compute Cost
    let computeCost(X:Matrix, y:Matrix, theta:Matrix) : float = 
        //J = sum(sum(((X*theta) - y)' * ((X*theta) - y))) / (2 * m);
        let x = X * theta - y
        Matrix.sum (x.Transpose() * x) / (2.0 * float X.RowCount)
    
    /// Gradient Descent Step
    let step (X:Matrix) (y:Matrix) (alpha:float) (theta:Matrix) : Matrix = 
        // theta - (((tr(X) * (X * theta - y)) * (alpha / float X.NumRows)))
        theta - (((X.Transpose() * (X * theta - y)) * (alpha / (float X.RowCount))))

    /// Linear Regression
    let regression (X:Matrix) (y:Matrix) (theta:Matrix) (alpha:float) =
        Seq.iterate (step X y alpha) theta
            |> Seq.map (fun theta -> computeCost(X,y,theta),theta)
