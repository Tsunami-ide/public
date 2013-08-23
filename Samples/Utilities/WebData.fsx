module Tsunami.Public
open System
open System.Net
open System.IO

type WebData() =
    static let wc = new WebClient()

    static let cacheDir = 
        lazy 
            let tmpDir = Path.GetTempPath()
            let cacheDir = Path.Combine([|tmpDir;"Tsunami";"Cache"|])
            Directory.CreateDirectory(cacheDir) |> ignore
            cacheDir
    static let md5Hash = System.Security.Cryptography.MD5.Create() 
    
    static let getHash (url:string) =
            Path.Combine(cacheDir.Force(),BitConverter.ToString(md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url))).Replace("-",""))
        
        
    static member ReadAllData(url:string) : byte[] =    
        let hashPath = getHash url
        if not <| File.Exists(hashPath) then wc.DownloadFile(url,hashPath)
        File.ReadAllBytes(hashPath)
            
    static member ReadAllText(url:string) = 
        let hashPath = getHash url
        if not <| File.Exists(hashPath) then wc.DownloadFile(url,hashPath)
        File.ReadAllText(hashPath)
        
    static member ReadAllLines(url:string) =
        let hashPath = getHash url
        if not <| File.Exists(hashPath) then wc.DownloadFile(url,hashPath)
        File.ReadAllLines(hashPath)
