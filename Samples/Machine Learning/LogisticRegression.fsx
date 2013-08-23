[<AutoOpen>]
module LogisticRegression

#load @"../Utilities/Utilities.fsx"
#load @"MathNet.fsx"

open MathNet.Numerics.LinearAlgebra.Double
open Tsunami.Public.MathNet
open Tsunami.Public.Utilities

module LogisticRegression =
    let sigmoid = Matrix.sigmoid

    /// CostFunctionReg
    let costFunctionReg(X:Matrix,y:Matrix, theta:Matrix, lambda:float) =
        //m = length(y); 
        let m = float X.RowCount
        // theta_prime = theta;
        let theta_prime = DenseMatrix.OfMatrix(theta)
        //theta_prime(1) = 0;
        theta_prime.[0,0] <- 0.;
        //gz = sigmoid(X * theta);
        let gz = Matrix.sigmoid(X * theta)
        // J = (sum((-y .* log(gz)) - ((1 - y) .* log(1 - gz))) / m) + (sum(theta_prime .^ 2) * (lambda / (2 * m))) ;
        let J = Matrix.sum ((-y .* Matrix.log(gz)) - ((-y .+ 1.) .* Matrix.log(-gz .+ 1.))) / m +
                  (Matrix.sum (theta_prime .* theta_prime) * (lambda / (2. * m)))
        //grad = (((gz - y)' * X)' ./ m) + (theta_prime .* (lambda / m));
        let grad = (tr(tr(gz - y) * X) ./ m) + (theta_prime * (lambda / m))
        (J,grad)

    let mapFeature (degree:int) (x1:Vector,x2:Vector) : Matrix =
        [| yield Vector.ones x1.Count
           for i in 1..degree do
             for j in 0..i do
                // out(:, end+1) = (X1.^(i-j)).*(X2.^j);
                yield (x1 .^ (float (i - j))) .* (x2 .^ float j)|]
        |> DenseMatrix.ofSeq
        |> tr

    /// Gradient Descent Step
    let step (X:Matrix) (y:Matrix) (alpha:float) (theta:Matrix) : Matrix = 
        theta - (tr(X) * (sigmoid(X * theta) - y)) * (alpha / float X.RowCount)

    /// Linear Regression
    let regression theta (X:Matrix) (y:Matrix) (alpha:float) (lambda:float) =
        //let theta = Matrix.zeros X.ColumnCount y.ColumnCount
        Seq.iterate (step X y alpha) theta 
            |> Seq.map (fun theta -> costFunctionReg(X,y,theta,lambda),theta)
            