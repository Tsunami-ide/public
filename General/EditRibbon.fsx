#r "Tsunami.IDEDesktop.exe"
#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "Telerik.Windows.Controls.dll"
#r "Telerik.Windows.Controls.Navigation.dll"
#r "Telerik.Windows.Controls.Docking.dll" 
#r "Telerik.Windows.Controls.RibbonView.dll" 
#r "ActiproSoftware.SyntaxEditor.Wpf.dll"
#r "System.Xaml.dll"
#r "ActiproSoftware.Shared.Wpf.dll"
#r "ActiproSoftware.Text.Wpf.dll"
#r "ActiproUtilities.dll"

open Tsunami.IDE
open Tsunami.Utilities
open System
open System.Windows
open System.Windows.Controls
open ActiproSoftware.Windows.Controls
open Telerik.Windows.Controls
open Telerik.Windows.Controls.RibbonView

let ui = Threading.DispatcherSynchronizationContext(Tsunami.IDE.UI.Instances.ApplicationMenu.Dispatcher)

async {
    do! Async.SwitchToContext ui
    let button = RadRibbonButton(Text = "Run", Size = ButtonSize.Large) 
    button.Click.Add(fun _ -> MessageBox.Show("Hello world") |> ignore)
    let tab = button |> addItem (RadRibbonGroup(Header = "Group")) |> addItem (RadRibbonTab(Header = "Tab"))    
    Tsunami.IDE.UI.Instances.RibbonView.Items.Add(tab) |> ignore
} |> Async.RunSynchronously