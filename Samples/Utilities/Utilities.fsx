module Tsunami.Public.Utilities

module Array = 
    /// Like Seq.groupBy, but returns arrays 
    let groupBy f (xs:_[]) = xs |> Seq.groupBy f |> Seq.map (fun (k,v) -> Seq.toArray v) |> Seq.toArray

    /// Add a set of vectors, pointwise
    let vectorSum (xss:float<_>[][]) = xss |> Array.reduce (Array.map2 (+))

    /// Like Seq.take, but returns an array
    let take n xs = Seq.take n xs |> Seq.toArray

    /// Like Seq.truncate, but returns an array
    let truncate (n:int) (xs:'a[]) = xs.[0.. (min (n-1) (xs.Length-1))]

    /// Compute the norm distance between two vectors
    let distance (xs:float<_>[]) (ys:float<_>[]) =
        (xs,ys) ||> Array.map2 (fun x y -> (x-y) * (x-y))
                |> Array.sum

    /// Find the average of set of vectors. First compute xs1 + ... + xsN, pointwise, 
    /// then divide each element of the sum by the number of vectors.
    let average (xss:float<'u>[][]) =
        xss 
          |> vectorSum
          |> Array.map (fun x -> x / float xss.Length)

    let countBy f (xs:'a[]) = xs |> Seq.countBy f |> Seq.toArray
    let head (xs:'a[]) = xs.[0]
    
    let mode (xs:'a[]) : 'a = 
            xs 
            |> countBy id 
            |> Array.sortBy snd 
            |> Array.rev 
            |> head
            |> fst

module Seq = 
    /// Return x, f(x), f(f(x)), f(f(f(x))), ...
    let iterate f x = x |> Seq.unfold (fun acc -> Some (acc, f acc))

    let lengthBy f s = s |> Seq.filter f |> Seq.length

