/// First we reference the Tsunami exe. 
/// This holds the type providers we will use.
#r "Tsunami.IDEDesktop.exe"

/// Then we bring the type providers into scope.
open Tsunami.IDE.TypeProvider

/// Now we instantiate the Facebook type provider.
/// It takes an access token as a parameter, which it uses
/// to connect to the graph.
///
/// You can generate an access token here: 
///     http://developers.facebook.com/tools/explorer/
///
/// You can control how much data you wish to expose to through the API.
/// You need to turn most of it on for interesting data.
type MyFace = FacebookTypeProvider< "PUT YOUR ACCESS TOKEN HERE" >
let myFace = MyFace()

/// You can use intellisense to explore your data (and your friend's data)
/// through the graph property. 
///
/// Intellisense in Tsunami can include images and tables so you can even see
/// the profile pictures of your friends and a property grid when you
/// highlight their name.

//myFace.graph.me

//myFace.graph.friends.``John Smith``

/// Using the graph model you can also post to feeds.

//myFace.me.TryPostToFeed "Posted this using Tsunami.io" |> ignore<bool>

/// The graph property exposes individual types for each person that contain
/// only the fields that the graph API returns for them. This makes it great
/// for exploring but harder to code against
///
/// Whereas, the "me" and "friends" properties on "myFace" show shared types that 
/// contain optional fields. This is good for coding against but harder to explore.
///
/// You can use the non-exploratory domain to do things like see if any of your
/// friends have a birthday this month.

let (|Int|_|) x =
    match System.Int32.TryParse x with
    | true, v -> Some v
    | false, _ -> None

let birthdaysThisMonth =
    myFace.friends.all |> Array.choose (fun f ->
        f.birthday |> Option.bind (fun b -> 
            f.name |> Option.map (fun n -> b, n)))
    |> Array.choose (fun (b, name) ->
        // Note: American date representation (sometimes with year)
        match b.Split [| '/'; '\\'; '.' |] with
        | [| Int month; Int day |] -> Some(day, month, "??", name)
        | [| Int month; Int day; year |] -> Some(day, month, year, name)
        | _ -> None)
    |> Array.filter (fun (_, month, _, _) -> month = System.DateTime.Now.Month)
    |> Array.map (fun (day, month, year, name) -> sprintf "%i/%i/%s" day month year, name)

for date, name in birthdaysThisMonth do
    printfn "%s\t\t\t%s" name date

