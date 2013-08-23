module ChatServer

#r "Tsunami.IDEDesktop.exe"
#r "PresentationFramework"
#r "PresentationCore"
#r "System.Xml.Linq"
#r "System.Reactive.Linq"
#r "System.Reactive.Core"

open System
open System.Net
open System.Net.Sockets
open Tsunami.Utilities
open Tsunami.Server.Base
open Tsunami.Server.AsyncHTTP
open Tsunami.Server.WebSockets

module Model =
    let mutable chatclient = html5Page
                                [ title "States"
                                  Bootstrap.css2_3_2
                                  cssScriptLink @"files/jarvis-widgets.css"
                                  cssScriptLink @"files/theme.css"
                                ]
                                [ attrs(["id","body"])
                                  JQuery.jquery1_9_1
                                  script (src "chatclient.js")
                                  Bootstrap.js2_3_2
                                  ]
    let mutable chatclientjs = ""

[<ReflectedDefinition>]
type C2S =
    | Login of string * string
    | Logout
    | Broadcast of string

[<ReflectedDefinition>]
type S2C =
    /// (user name, gravatar url, message text)
    | Msg of string * string * string 
    | Error of string

type Message<'T,'V> = 
    | S2C of 'T
    | C2S of 'V
    | Drop

let clients = new System.Collections.Concurrent.ConcurrentDictionary<Guid,Agent<Message<S2C,C2S>>>()

let chat (state: HTTPState) : Async<unit> =
    async {
        let ns = state.client.GetStream()
        let agent = MailboxProcessor.Start(fun (inbox:MailboxProcessor<Message<S2C,C2S>>) -> 
            let send (s:S2C) = inbox.Post(S2C s)
            let sendTo (a: Agent<_>) s = a.Post (S2C s)
            let guid = Guid.NewGuid()
            clients.[guid] <- inbox
            
            let rec LoggedOut () =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | S2C s ->
                        do! wrapJS s ns
                        return! LoggedOut ()
                    | C2S (Login (user,email)) ->
                        let gravatarHash =
                            email.Trim().ToLower()
                            |> System.Text.Encoding.UTF8.GetBytes
                            |> md5.ComputeHash
                            |> Seq.map (fun b -> sprintf "%02x" b)
                            |> String.concat ""
                        let gravatarUrl = "http://www.gravatar.com/avatar/" + gravatarHash + "?s=32"
                        return! LoggedIn (user,gravatarUrl)
                    | Drop ->
                        clients.TryRemove guid |> ignore
                        return ()
                    | C2S (Broadcast _)
                    | C2S (Logout _) ->
                        send (Error "Message received that would require client to be logged in")
                        return! LoggedOut ()
                }
            
            and LoggedIn (username,gravatar) = 
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | S2C s ->
                        do! wrapJS s ns
                        return! LoggedIn (username,gravatar)
                    | C2S (Broadcast s) ->
                        for agent in clients.Values
                            do sendTo agent (Msg (username, gravatar, s))
                        return! LoggedIn (username,gravatar)
                    | C2S Logout ->
                        return! LoggedOut ()
                    | C2S (Login _) ->
                        send (Error "Already logged in")
                        return! LoggedIn (username,gravatar)
                    | Drop ->
                        clients.TryRemove guid |> ignore
                        return ()
                }
            LoggedOut ()
            )
        
        // Handle the actual websocket Network Stream and
        // forward on each message received to the agent
        let rec loop() =
            async {
                try
                    let! recv = unwrapJS<C2S> ns
                    //printfn "Recv: %A" recv
                    agent.Post(C2S recv)
                    return! loop()
                with 
                | ex ->
                    sprintf "%s dropped error: %s" (state.client.Client.RemoteEndPoint.ToString()) (ex.ToString()) |> log
                    agent.Post (S2C (Error "Server received malformed message, dropping client."))
                    //agent.Post Drop
                    return ()
            }
        return! loop()

    }

let getFile directory : AsyncWebParser<string,unit> =
    fun (fileName,input) ->
        async {
            try
                let fileName = fileName.Split('?').[0]
                printfn "filename: %s" fileName
                let bytes = IO.File.ReadAllBytes (IO.Path.Combine (directory,fileName))
                let reply = { input with
                                response = { input.response with
                                                status = 200
                                                mimeType = Some (MimeTypes.getDefaultMime fileName)
                                                headers = Map.empty
                                                body = Some bytes
                                                closeConnection = true }
                            }
                return (True ((),reply))
            with
            | _ -> return (False input)
        }

let routing : AsyncParser<HTTPState,string,string,string> =
        let websockets = 
            [
                getPath "/chat" |> respondWebsocket chat
            ] |> disjoin
            
        let website = 
            [
                getPath "/"  -!> okDynamic(fun _ -> Model.chatclient)
                getPath "/chatclient.js"  -!> okDynamicWithMimeType "application/javascript" (fun _ -> Model.chatclientjs)
                getStartsWith "/files/" -!> getFile (IO.Path.Combine (__SOURCE_DIRECTORY__,"files"))
                notFound
            ] 
            |> disjoin
            |> respondWeb

        websockets <|> website

let chatServer() = webserver "Chat Server" IPAddress.Any 4503 routing None
