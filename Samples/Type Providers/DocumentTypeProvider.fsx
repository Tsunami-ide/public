/// First we reference the Tsunami exe. 
/// This holds the type providers we will use.
#r "Tsunami.IDEDesktop.exe"

/// Then we bring the type providers into scope.
open Tsunami.IDE.TypeProvider

/// Now we can create a shortcut (or typedef) to the document type provider.
type Docs = DocumentTypeProvider

/// This type provider provides type safe access to the documents
/// that are currently open in Tsunami.
let thisDoc =  Docs.``DocumentTypeProvider.fsx``

/// The type provider supports invalidation too.
/// Try it yourself. Add a new script in the IDE and see it appear in intellisense below.
let newScript = Docs.``DocumentTypeProvider.fsx`` // <-- Change me!

/// We can get a list of all open documents and the shell document too.
let allDocs = DocumentTypeProvider.AllDocuments
let shell = DocumentTypeProvider.ShellDocument

/// From the document we can do things like:

/// 1. See some document properties
printfn "Url: %s" thisDoc.Url
printfn "SelectedText: %s" thisDoc.SelectedText
printfn "IsModified: %b" thisDoc.IsModified
printfn "IsReadOnly: %b" thisDoc.IsReadOnly

/// 3. Modify the script.
thisDoc.Text <- thisDoc.Text + "//Replaced a comment"

/// 4. Find and replace using code.
thisDoc.Text <- thisDoc.Text.Replace("//Replaced a comment", "//Replaced a comment")

/// 5. Run the script or part of it.
let canRunSelection = thisDoc.CanRunSelectedText()
let canRun = thisDoc.CanRun()
// thisDoc.RunSelectedText()
// thisDoc.Run()
