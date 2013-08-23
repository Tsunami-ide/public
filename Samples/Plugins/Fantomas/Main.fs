module Main

open System.Windows.Input
open System.Windows
open System.Windows.Controls
open System
open System.IO
open System.Diagnostics
open Tsunami.Utilities.WPF
open Tsunami.Utilities

type FantomasPlugin() =

    let formatCurrentFile () =
        match !!Tsunami.IDE.DocumentSystemViewModel.Instance.ActiveDocument with
        | None
        | Some None -> ()
        | Some (Some doc) ->
            match !!doc.Text with
            | None -> ()
            | Some (_, text) ->
                try
                    let text' = Fantomas.CodeFormatter.formatSourceString false text Fantomas.FormatConfig.FormatConfig.Default
                    doc.Text <-- (Tsunami.IDE.FromOther, text')
                with
                | e -> System.Windows.MessageBox.Show (sprintf "Fantomas plugin exception:\n%s" e.Message, "Exception: '" + e.ToString() + "'.") |> ignore

    interface Tsunami.IDE.Extensions.PluginInterface with
        member val name = "Fantomas Plugin"
        
        member this.run () =
            Tsunami.IDE.Simple.addButton("Fantomas Code Format", formatCurrentFile)

            // Warm up the JIT
            async {
                Fantomas.CodeFormatter.formatSourceString false "" Fantomas.FormatConfig.FormatConfig.Default
                |> ignore
            } |> Async.Start
