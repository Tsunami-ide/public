#r "cache:http://tsunami.io/assemblies/FunScript.dll"
#r "cache:http://tsunami.io/assemblies/FunScript.TypeScript.dll"
#r "cache:http://tsunami.io/assemblies/FunScript.TypeScript.Interop.dll"
#load @"ChatServer.fsx" 

open System
open System.IO
open FunScript
open FunScript.TypeScript
open System.Threading



[<Literal>]
let ts = """http://tsunami.io/TypeScript/jquery.d.ts
            http://tsunami.io/TypeScript/bootstrap.d.ts
          """

type j = TypeScript.Api<ts>

type Async =
  static member AwaitJQueryEvent(f : ('T -> obj) -> j.JQuery) : Async<'T> = 
    Async.FromContinuations(fun (cont, econt, ccont) ->
      let named = ref None
      named := Some (f (fun v -> 
        (!named).Value.off() |> ignore
        cont v
        obj() )))
 
[<FunScript.JS>]
module JQuery =
    let prepend (x:j.JQuery) (y:j.JQuery) = y.prepend([|box x|])
    let append (xs:j.JQuery[]) (y:j.JQuery) = y.append([|for x in xs do yield box x|]) |> ignore; y
    let nestedAppend (xs:j.JQuery[]) (y:j.JQuery) = 
        let nest (x:j.JQuery) (y:j.JQuery) =
            y.append([|box x|]) |> ignore; x
        xs |> Seq.fold (fun state x -> nest x state) y |> ignore
        y

    let after (x:j.JQuery) (y:j.JQuery) = x.insertAfter(box y) |> ignore; y
    let before (x:j.JQuery) (y:j.JQuery) = x.insertBefore(box y) |> ignore; y

    let addAttr (attr:string) (value:string) (x:j.JQuery) = x.attr(attr,value) |> ignore; x
    let addClass (``class``:string) (x:j.JQuery) = x.addClass(``class``) |> ignore; x
    let setId (id:string) (x:j.JQuery) = x.attr("id",id) |> ignore; x
    let onClick (f:unit->unit)  (x:j.JQuery) = 
            x.click(Func<j.JQueryEventObject,obj>(fun _ -> f(); box null)) |> ignore
            x

    let hide(x:j.JQuery) = x.hide()
    let show(x:j.JQuery) = x.show()
    
    let onSubmit (f:unit->unit)  (x:j.JQuery) =
        x.submit(Func<j.JQueryEventObject,obj>(fun _ -> f(); box null))

    let html (s:string) (j:j.JQuery) = j.html s |> ignore; j

    let attrs (xs:(string*string)[]) (jobj:j.JQuery) = 
            xs |> Array.iter (fun (name,value) -> jobj.attr(name,value) |> ignore)
            jobj

[<FunScript.JS>]
module Bootstrap =
    let showModal (x:j.JQuery) = x.modal("show")
    let hideModal (x:j.JQuery) = x.modal("hide")

[<FunScript.JS>]
module WebSocket =
    type IWebSocket =
        abstract send : string -> unit
        abstract close : unit -> unit
        
    [<JSEmit("""
        socket = new WebSocket({0});
        socket.onopen = function () { 
            {1}();
        };

        socket.onmessage = function (msg) {
            {2}(msg.data);
        };

        socket.onclose = function () {
            {3}();
        };

        return socket;""")>]
    let createImpl(host : string, onOpen : unit -> unit, onMessage : string -> unit, onClosed : unit -> unit) : IWebSocket = 
        failwith "never"

    let create(host, onMessage, onClosed) =
        Async.FromContinuations (fun (callback, _, _) ->
            let socket = ref Unchecked.defaultof<_>
            socket := createImpl(host, (fun () -> callback !socket), onMessage, onClosed)
        )

[<AutoOpen>]
[<FunScript.JS>]
module Utilities =
    [<JS; JSEmit("""return {1}[{0}];""")>]
    let field<'a> (y:string,x:obj) : 'a = failwith "never"

    let (?) (x:'a) (name:string) = field(name,x)


[<FunScript.JS>]
module String =
    [<JSEmit """return (!{0} || /^\s*$/.test({0}));""">]
    let IsNullOrWhiteSpace (s: string) : bool = failwith "never"
        
[<FunScript.JS>]
module TsunamiStateMachine =    
    let jQuery (command:string) = j.jQuery.Invoke(command)
    let ignore _ = ()
    let (-->) (x:j.JQuery) (y:j.JQuery) = x.append([|box y|]) |> ignore; y
    let (<--) (x:j.JQuery) (y:j.JQuery) = x.append([|box y|]) |> ignore; x
    
    type State = 
        | Login
        | Messages of string * string   // username, email
        
    type Message =
        | LoginM of string * string     // username, email
        | LogoutM

    [<JSEmit "window.alert({0});">]
    let alert (s: string) : unit = failwith "never"
    
//    [<JSEmit("{0}.animate({ scrollTop : {0} });")>]
//    let animateScrollBottom(x:j.JQuery) : unit = failwith "never"
    
    [<JSEmit("""$('.chat-messages').scrollTop($('.chat-messages')[0].scrollHeight);""")>]
    let scrollChatToBottom() : unit = failwith "never"
    
    type Model() =

        let loginLink = 
            jQuery("""<li><a href="#">Login</a></li>""")
        
        let logoutLink = 
            jQuery("""<li><a href="#">Logout</a></li>""")
           |> JQuery.hide

        let email = jQuery("<input>") |> JQuery.attrs [| "type", "text"; "placeholder", "Email" |]

        let username = jQuery("<input>") |> JQuery.attrs [| "type", "text"; "placeholder", "Username" |]

        let login_button = jQuery("""<a href="#" class="btn btn-primary">Login</a>""")
        let close_login_button = jQuery("""<a href="#" class="btn" data-dismiss="modal">Close</a> """)

        let loginPanel =
                jQuery("""<div class="modal hide fade" tabindex="-1"  id="addIdea">""")
                |> JQuery.append [|
                    jQuery("""<div class="modal-header">""")
                    |> JQuery.append [|
                        jQuery("""<button class="close" data-dismiss="modal">x</button>""")
                        jQuery("""<h3>Login to Chat</h3>""")
                        |]
                    jQuery("""<div class="modal-body">""")
                    |> JQuery.append [|
                        email
                        jQuery("<br>")
                        username
                        jQuery("<br>")
                        |]
                    jQuery("""<div class="modal-footer">""")
                    |> JQuery.append [| 
                      login_button
                      close_login_button
                      |]
                |]
    
        // Add Messages
        let chatMessages = jQuery("""<div class="chat-messages" style="width: auto; height: 370px;">""")
                    
        let header = jQuery("<h1>Messages Panel</h1>")
        
        let send_button =
            jQuery("""<input type="submit" name="chat-box-textarea" class="btn medium btn-danger pull-right" value="Send" id="send-msg-js">""")
        
        let msgText = jQuery("""<textarea name="enter-message" rows="3" cols="1" placeholder="Enter your message..." id="chat-box-textarea"></textarea>""")

        let messages_widget =

            let chat =
                jQuery("""<div class="tab-content chat-content">""") 
                |> JQuery.nestedAppend [|
                    jQuery("""<div class="tab-pane fade in active" id="user1">""")
                    jQuery("""<div class="slimScrollDiv" style="position: relative; overflow: hidden; width: auto; height: 370px;">""")
                    chatMessages
                    |]
                
            jQuery("<div>")
            |> JQuery.nestedAppend [| 
                jQuery("""<div role="content">""")
                jQuery("""<div class="inner-spacer">""")
                |> JQuery.append [|
                        chat
                        jQuery("<div>")
                        |> JQuery.addClass "row-fluid chat-box"
                        |> JQuery.append [|
                            msgText
                            jQuery("""<div class="row-fluid">""")
                            |> JQuery.append [|
                                jQuery("""<div class="span6 chat-box-buttons pull-right">""")
                                |> JQuery.append [| send_button |]
                            |]
                        |]
                    |]
                |]
            
        let messagesPanel =
            let h = JQuery.addClass "page-header" (jQuery("<div>")) 
                    |> JQuery.append [|header|]

            JQuery.addClass "container" (jQuery("<div>"))  
            |> JQuery.append  [|h; messages_widget;  |]
            
        let mutable state = State.Login
        let mutable connection = Option<WebSocket.IWebSocket>.None
        let recvMsg(msg:string) : unit =
            match state with
            | State.Messages(user,email) ->
                match Tsunami.SerDes.FunScript.fromJSON msg with
                | ChatServer.Msg (username,gravatarUrl,text) ->
                    let msg = jQuery("<p>")
                                |> JQuery.addClass ("message-box" + if user = username then " you" else "")
                                |> JQuery.append
                                    [|
                                        jQuery("<img>") |> JQuery.addAttr "src" gravatarUrl
                                        jQuery("<span>")
                                        |> JQuery.addClass "message"
                                        |> JQuery.append
                                                [|
                                                    jQuery("<strong>")
                                                        |> JQuery.html username
    
                                                    jQuery("<span>")
                                                        |> JQuery.addClass "message-text"
                                                        |> JQuery.html text
                                                |]
                                    |]
                    chatMessages |> JQuery.append [|msg|] |> ignore
                    scrollChatToBottom()
                | ChatServer.Error s -> alert s

            | _ -> ()
            
        let sendMsg = ref (fun (msg:string) -> recvMsg("outgoing message ignored " + msg))

        member this.ShowLoginDialog() = loginPanel |> Bootstrap.showModal

        member this.RecvMsg(msg:ChatServer.S2C) = recvMsg( Tsunami.SerDes.FunScript.toJSON msg)

        member this.transist(msg:Message) : unit =
            let newState = 
                match state with
                | Login -> 
                    match msg with
                    | LoginM(username, email) -> 
                        if String.IsNullOrWhiteSpace username || String.IsNullOrWhiteSpace email then
                          alert "Please provide details"
                          None
                        else                   
                          Some(Messages(username, email))
                    | _ -> None
                | Messages(_) ->
                    match msg with
                    | LogoutM -> Some(Login)
                    | _ -> None
                
            if newState.IsSome then
                state <- newState.Value 
                match newState.Value with
                | Login -> this.login_panel()
                | Messages(user, email) -> this.messages_panel(user, email)

        member this.login_panel() = 
            logoutLink |> JQuery.hide |> ignore
            loginLink |> JQuery.show |> ignore
            chatMessages.empty() |> ignore
            messagesPanel |> JQuery.hide |> ignore            

        member this.messages_panel(user:string, email:string) =
                logoutLink |> JQuery.show |> ignore
                loginLink |> JQuery.hide |> ignore
                loginPanel |> Bootstrap.hideModal |> ignore
                
                chatMessages.empty() |> ignore
                header.text("Logged in as " + user) |> ignore

                messagesPanel |> JQuery.show |> ignore

                async {
                        let! websocket = WebSocket.create("ws://127.0.0.1:4503/chat", recvMsg, fun _ -> this.transist(LogoutM) )
                        connection <- Some(websocket)
                        websocket.send(Tsunami.SerDes.FunScript.toJSON (ChatServer.Login (user,email)))
                        sendMsg := fun msg -> websocket.send(Tsunami.SerDes.FunScript.toJSON (ChatServer.Broadcast msg))
                } |> Async.StartImmediate  

        member this.Logout() = 
                    match connection with
                    | Some(c) -> c.close()
                    | _ -> ()
                    this.transist(LogoutM)
        member this.LoginLink = loginLink
        member this.LogoutLink = logoutLink
        member this.init() =     
                loginLink |> JQuery.onClick (fun _ -> this.ShowLoginDialog() |> ignore) |> ignore
                logoutLink|> JQuery.onClick (fun _ -> this.Logout())  |> ignore

                login_button
                |> JQuery.onClick (fun _ -> this.transist(LoginM(username.``val``() :?> string, email.``val``() :?> string)))
                |> ignore

                send_button
                |> JQuery.onClick (fun _ -> !sendMsg (msgText.``val``() :?> string))
                |> ignore

                jQuery("<div>")
                |> JQuery.setId "chat"
                |> JQuery.append [| loginPanel |> JQuery.show; messagesPanel |> JQuery.hide |]

    let main() = 
        let m = Model()

        let navbarIcon = 
            jQuery("""<div class="brand scroller" >Chat</div>""")
            |> JQuery.append (Array.init 3 (fun _ -> jQuery("""<span class="icon-bar">""")))
    
        let navbar(brand,inner) =
            jQuery("""<div class="navbar navbar-fixed-top">""") 
            |> JQuery.append [| 
                jQuery("""<div class="navbar-inner">""")
                |> JQuery.append [|
                    jQuery("""<div class="container">""")
                    |> JQuery.append [|
                        brand
                        jQuery("""<div class="nav-collapse collapse" id="main-menu">""")
                        |> JQuery.append [|
                            jQuery("""<ul class="nav pull-right">""") 
                            |> JQuery.append inner
                        |]
                    |]
                |]
            |]

        let inner = [| m.LoginLink; m.LogoutLink |]

        jQuery("#body")
        |> JQuery.append [| 
            navbar(navbarIcon,inner)
            m.init(); 
            |]
        |> ignore

let components = 
  FunScript.Interop.Components.all


let js = FunScript.Compiler.Compiler.Compile(<@ TsunamiStateMachine.main() @>, components=components, noReturn = true)
ChatServer.Model.chatclientjs <- js

let server = ChatServer.chatServer()
System.Diagnostics.Process.Start("http://127.0.0.1:4503") 
//server.Dispose()

// Broadcast a message to all clients
open ChatServer

for client in clients do
    client.Value.Post(S2C(S2C.Msg("example@gmail.com","Example","Hello!")))