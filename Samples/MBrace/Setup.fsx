// Run this in Tsunami FSI
#r "Tsunami.IDEDesktop.exe"
open Tsunami.Utilities
open Tsunami.IDE

Dispatcher.invoke(fun _ -> 
    Simple.runFSI()
    Simple.runProcess(@"C:\Program Files (x86)\MBrace\bin\mbi.exe","")    
    )