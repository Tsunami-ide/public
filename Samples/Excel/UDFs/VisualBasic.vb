Namespace ClassLibrary1
    Public NotInheritable Class Class1
        Private Sub New()
        End Sub
        Public Shared Function fAdd3(x As [Double], y As [Double]) As [String]
            Return x + y + 3.0
        End Function

        Public Shared Function fMult3(x As [Double]) As [Double]
            Return x * 3.0
        End Function

        Public Shared Function fSquare(x As [Double]) As [Double]
            Return x * x
        End Function

    End Class
End Namespace