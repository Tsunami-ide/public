/// First we reference the Tsunami exe. 
/// This holds the type providers we will use.
#r "Tsunami.IDEDesktop.exe"

/// Then we bring the type providers into scope.
open Tsunami.IDE.TypeProvider

/// Now we instantiate the NuGet type provider.
/// The parameter tells the provider where it can find the NuGet metadata.
/// If it isn't supplied it will use its default.
type Nu = NuGetTypeProvider< "" >

/// From the provider we can filter all the NuGet packages
/// by their tags using intellisense.
type Tags = Nu.TagBrowser.AllTags

/// This returns a node with all the packages that are tagged with "fsharp".
/// The number indicates the number of packages with this tag.
type FSharpTag = Tags.``fsharp  79`` // <- Try me!

/// This returns a node with all the packages that are tagged with "nunit".
/// The number indicates the number of packages with this tag.
type NUnitTag = Tags.``nunit  47``

/// These both return a node with all the packages that are tagged with "fsharp" AND "nunit".
/// The first starts filtering with "fsharp" whereas the second starts with "nuget" but they
/// both end up with only 3 packages.
type FSharpAndNUnit = Tags.``fsharp  79``.``nunit  3``
type NUnitAndFSharp = Tags.``nunit  47``.``fsharp  3``

/// From any node we can pull out the packages that match the filter so far.
type FSharpPackages = FSharpTag.Packages

/// We can use intellisense to find the package we want.
/// The number here indicates how many downloads the package
/// has had at the instance the metadata was collected.
let fsharpData = FSharpPackages.``FSharp.Data  1170`` // <- Try me!

/// From a package we can find out its details.
let details = fsharpData.package_details
printfn "Id: %s" details.id
printfn "Description: %s" details.description
printfn "LastUpdate: %A" details.lastUpdate

/// We can use intellisense to investigate its dlls.
let mainDll = fsharpData.dlls.``FSharp.Data.dll`` // <- Try me!
let designTimeDll = fsharpData.dlls.``FSharp.Data.DesignTime.dll``

/// We can get a list of all of the dlls.
printfn "Dlls:"
for dll in fsharpData.dlls.all do
    printfn "   %s" dll
