#r "Tsunami.IDEDesktop.exe"
#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "Telerik.Windows.Controls.dll"
#r "ActiproSoftware.Shared.Wpf.dll"
#r "ActiproSoftware.SyntaxEditor.Wpf.dll"
#r "ActiproSoftware.Docking.Wpf.dll"
#r "ActiproSoftware.Ribbon.Wpf.dll"
#r "ActiproSoftware.DataGrid.Contrib.Wpf.dll"
#r "System.Xaml.dll"

open System.Windows
open System.Windows.Controls
open ActiproSoftware.Windows.Controls.Ribbon
open Tsunami.IDE

let ui = Threading.DispatcherSynchronizationContext(UI.Instances.ApplicationMenu.Dispatcher)

async {
    do! Async.SwitchToContext ui
    let button = Button(Width = 70.)
    button.Content <- "Run" 
    button.Click.Add(fun _ -> MessageBox.Show("Hello world") |> ignore)
    let tab = Controls.Tab(Label = "Tab")
    let group = Controls.Group(Label = "Group")
    group.ItemsSource <- [|button|]
    tab.Items.Add group
    UI.Instances.RibbonView.Tabs.Add tab
} |> Async.RunSynchronously