module Tsunami.PhoneGap

open System
open System.IO
open System.Net
open System.Text
open Tsunami.Utilities
open Newtonsoft.Json
open Newtonsoft.Json.Linq

//ServicePointManager.ServerCertificateValidationCallback <- new System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)

type AppType =
  | Bytes of byte[] * string
  | Remote of string

type KeyApp =
    | Android of int * string * string // id, key_pw, keystore_pw
    | OtherOS of string * int * string // os, id, key_pw

let zipDirToBytes d =
    use zip = new Ionic.Zip.ZipFile()
    zip.AddDirectory d |> ignore
    use stream = new MemoryStream()
    zip.Save(stream)
    stream.ToArray()



let private keyAppToJSON (jw: JsonWriter) (k: KeyApp) =
      match k with
      | Android (keyid, key_pw, keystore_pw) ->
          jw.WritePropertyName("android")
          jw.WriteStartObject()
          jw.WritePropertyName("id"); jw.WriteValue(keyid)
          jw.WritePropertyName("key_pw"); jw.WriteValue(key_pw)
          jw.WritePropertyName("keystore_pw"); jw.WriteValue(keystore_pw)
          jw.WriteEndObject()
      | OtherOS (os, keyid, password) ->
          jw.WritePropertyName(os)
          jw.WriteStartObject()
          jw.WritePropertyName("id"); jw.WriteValue(keyid)
          jw.WritePropertyName("password"); jw.WriteValue(password)
          jw.WriteEndObject()

type private UpdateApp =
    {
        title: Option<string>
        package: Option<string>
        version: Option<string>
        description: Option<string>
        debug: Option<bool>
        keys: Option<List<KeyApp>>
        ``private``: Option<bool>
        phonegap_version: Option<string>
        hydrates: Option<bool>
        pull: Option<bool>
    }

let private updateAppToJSON (jw: JsonWriter) (x: UpdateApp) =
    Option.iter (fun (o: string) -> jw.WritePropertyName("title"); jw.WriteValue o) x.title
    Option.iter (fun (o: string) -> jw.WritePropertyName("package"); jw.WriteValue o) x.package
    Option.iter (fun (o: string) -> jw.WritePropertyName("version"); jw.WriteValue o) x.version
    Option.iter (fun (o: string) -> jw.WritePropertyName("description"); jw.WriteValue o) x.description
    Option.iter (fun (o: bool) -> jw.WritePropertyName("debug"); jw.WriteValue o) x.debug
    Option.iter (fun (o: bool) -> jw.WritePropertyName("private"); jw.WriteValue o) x.``private``
    Option.iter (fun (o: string) -> jw.WritePropertyName("phonegap_version"); jw.WriteValue o) x.phonegap_version
    Option.iter (fun (o: bool) -> jw.WritePropertyName("hydrates"); jw.WriteValue o) x.hydrates
    Option.iter (fun (o: bool) -> jw.WritePropertyName("pull"); jw.WriteValue o) x.pull
    match x.keys with
    | None -> ()    
    | Some m -> jw.WritePropertyName("keys")
                jw.WriteStartObject()
                List.iter (keyAppToJSON jw) m
                jw.WriteEndObject()

let private buildURL = @"https://build.phonegap.com/api/v1/"

let private send (meth: string, url: string, data: string, file: Option<byte[]*string>) =
    let req = HttpWebRequest.Create(url)
    req.Method <- meth        

    match meth with
    | WebRequestMethods.Http.Get -> ()
    | _ ->
        let boundary = "-------------" + System.DateTime.Now.Ticks.ToString()
        let start =
            let sb = StringBuilder()
            sb.Append("--").Append(boundary).Append("\r\n")
              .Append("Content-Disposition: form-data; name=\"data\"\r\n\r\n")
              .Append(data).Append("\r\n")
              .ToString()
            |> Encoding.ASCII.GetBytes

        let bss =
            match file with
            | None -> []
            | Some (bs, filename) ->

                let bs' =
                    let sb = new StringBuilder()
                    sb.Append("--").Append(boundary).Append("\r\n")
                      .Append("Content-Disposition: form-data; name=\"file\"; filename=\"")
                      .Append(filename).Append("\"").Append("\r\n")
                      .Append("Content-Type: ").Append(MimeTypes.getDefaultMime filename).Append("\r\n\r\n")
                      .ToString()
                    |> Encoding.ASCII.GetBytes
                [ bs'; bs ]

        let final = "--" + boundary + "--\r\n"
                    |> Encoding.ASCII.GetBytes


        req.ContentType <- "multipart/form-data; boundary=" + boundary
        req.ContentLength <- start.LongLength + final.LongLength
                              + List.sumBy (Array.length >> int64) bss

        let stream = req.GetRequestStream()
        stream.Write(start, 0, start.Length)
        for bs in bss do
            stream.Write(bs, 0, bs.Length)
        stream.Write(final, 0, final.Length)
        stream.Close()

    let resp =
        try
            req.GetResponse()
        with
        | :? System.Net.WebException as e -> e.Response

    let bytes = resp.GetResponseStream().ToBytes()
    Encoding.UTF8.GetString(bytes)

type Write() =
    
    /// POST https://build.phonegap.com/api/v1/apps
    static member createApp (token: string, title: string, create: AppType, ?filename, 
                             ?package, ?version, ?description, ?debug: bool, ?keys:List<KeyApp>, ?``private``, ?phonegap_version, ?hydrates) =
        
            use sw = new StringWriter()
            use jw = new JsonTextWriter(sw)
     
            jw.WriteStartObject()        
            jw.WritePropertyName("create_method");

            let sendfile =
                match create with
                | Bytes (v1,v2) ->
                    jw.WriteValue "file"
                    Some (v1,v2)

                | Remote repo ->
                    jw.WriteValue "remote_repo"
                    jw.WritePropertyName("repo"); jw.WriteValue repo
                    None

            {
              title = Some title
              package = package
              version = version
              description = description
              debug = debug
              keys = keys
              ``private`` = ``private``
              phonegap_version = phonegap_version
              hydrates = hydrates
              pull = None
            }
            |> updateAppToJSON jw

            jw.WriteEndObject()

            let url = sprintf "%sapps?auth_token=%s" buildURL token
            let data = sw.ToString()
            let response = send("POST", url, data, sendfile)

            try
                let o = JObject.Parse response
                match o.TryGetValue("id") with
                | true, id -> Success (id.Value<int>())
                | false, _ ->
                match o.TryGetValue("error") with
                | true, err -> Failed (err.ToString())
                | false, _ ->  Failed "unknown error"
            with
            | :? JsonReaderException -> Failed "unknown error"

    /// Update an existing app - the contents of the app, the app's metadata, or both
    /// PUT https://build.phonegap.com/api/v1/apps/:id
    static member updateApp (token: string, id: int, ?sendfile: byte[] * string,
                             ?title: string, ?package, ?version, ?description, ?debug: bool, ?keys:List<KeyApp>, ?``private``, ?phonegap_version, ?hydrates, ?pull) =

        use sw = new StringWriter()
        use jw = new JsonTextWriter(sw)

        jw.WriteStartObject()
        {
          title = title
          package = package
          version = version
          description = description
          debug = debug
          keys = keys
          ``private`` = ``private``
          phonegap_version = phonegap_version
          hydrates = hydrates
          pull = pull
        }
        |> updateAppToJSON jw
        jw.WriteEndObject()

        let url = sprintf "%sapps/%d?auth_token=%s" buildURL id token
        let data = sw.ToString()
        let response = send("PUT", url, data, sendfile)
        try
            let o = JObject.Parse response
            match o.TryGetValue("status") with
            | true, id -> Success ()
            | false, _ ->
            match o.TryGetValue("error") with
            | true, err -> Failed (err.ToString())
            | false, _ ->  Failed "unknown error"
        with
        | :? JsonReaderException ->  Failed "unknown error"

    static member deleteApp (token: string, id: int) =
        let url = sprintf "%sapps/%d?auth_token=%s" buildURL id token
        let response = send("DELETE", url, "", None)

        try
            let o = JObject.Parse response
            match o.TryGetValue("success") with
            | true, id -> Success ()
            | false, _ ->
            match o.TryGetValue("error") with
            | true, err -> Failed (err.ToString())
            | false, _ ->  Failed "unknown error"
        with
        | :? JsonReaderException ->  Failed "unknown error"

type Read() =

    static member keys (token: string) =
        let url = sprintf "%skeys?auth_token=%s" buildURL token
        let response = send("GET", url, "", None)

        try
            let o = JObject.Parse response
            try
                [ 
                  for c in (o.["keys"] :?> JObject).Properties() do
                      yield c.Name, [ for ks in c.Value.["all"].Children() do
                                        yield ks.["title"].ToString(), ks.["id"].Value<int>()
                                    ]
                ] |> Map.ofList |> Success
            with
            | :? NullReferenceException -> 
                match o.TryGetValue("error") with
                | true, err -> Failed (err.Value<string>())
                | false, _ ->  Failed "unknown error"
        with
        | :? JsonReaderException ->
            Failed "unknown error"

    static member apps (token: string) =
        let url = sprintf "%sapps?auth_token=%s" buildURL token
        let response = send("GET", url, "", None)

        try
            let o = JObject.Parse response
            try
                [ 
                  for c in (o.["apps"]) do
                    yield c.["title"].Value<string>(), c.["id"].Value<int>()
                ] |> Success
            with
            | :? NullReferenceException -> 
                match o.TryGetValue("error") with
                | true, err -> Failed (err.Value<string>())
                | false, _ ->  Failed "unknown error"
        with
        | :? JsonReaderException ->
            Failed "unknown error"