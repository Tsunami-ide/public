module ExcelUdfs
open System
open System.Net

let ``FS.HelloWorld``() = "Hello World from F#!"
let ``FS.getLastTrade``() : Async<float> = 
    async {
        use client = new WebClient()
        let url = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=l1&e=.csv" "MSFT"
        let! res = client.AsyncDownloadString(Uri(url))
        return float res
    }