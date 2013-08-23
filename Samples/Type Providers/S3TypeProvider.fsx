/// First we reference the Tsunami exe. 
/// This holds the type providers we will use.
#r "Tsunami.IDEDesktop.exe"

/// Then we bring the type providers into scope.
open Tsunami.IDE.TypeProvider

/// First we add a named connection to Tsunami's set of connections.
let accessKey = "PUT YOUR ACCESS KEY HERE"
let secret = "PUT YOUR SECRET HERE"
Tsunami.AWS.S3.Connections.add "MyS3Connection" accessKey secret

/// Then we instantiate the S3 type provider with the named connection.
/// Note: Intellisense won't work until you run the lines above.
type S3 = S3TypeProvider< "MyS3Connection" > 

/// The provider gives us a data context.
let dataContext = S3.GetDataContext()

/// From the data context we can use intellisense to navigate the
/// buckets on the S3 account.

//let bucket = dataContext.``mywebsite.com``

/// We can also get a list of all the buckets.

//let allBuckets = dataContext.Buckets

/// From an individual bucket we can navigate the directory structure
/// using intellisense. 
/// Note: The structure is actually flat but we use separators like '/' to 
/// define the structure.

//let javaScripts = bucket.``js/``

/// We can also get a list of all objects stored in the S3 bucket
/// with the directory prefix.

//let objects = javaScripts.Objects

/// From a bucket/folder we can navigate the files using intellisense.

//let myFile = javaScripts.``bootstrap.js``

/// From a file we can use properties such as Uri, LastModified and Content.

//printfn "Uri: %s" myFile.Uri
//printfn "LastModified: %A" myFile.LastModified
//printfn "Content:\n%s" (myFile.Content |> System.Text.Encoding.UTF8.GetString)