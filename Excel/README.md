## Excel Examples ##

####To install

Load the VSTO file at `C:\Program Files (x86)\Tsunami\ExcelFSharp.vsto`


####To execute

Ribbon -> View -> F# -> Tsunami

####Note

Currently Tsunami is loads when Excel loads and adds a noticeable lag to opening Excel. In the future this lag will be delayed to the first time Tsunami is invoked within Excel.

##Excel F# UDFs
####Prerequisite
[F# PowerPack](http://fsharppowerpack.codeplex.com/)

####Scripts
The script `StringToUdf.fsx` demonstrates the process for creating UDFs using inline strings.

The script `ShellToUdf.fsx` demonstrates the process for creating UDFs using the in memory Shell.fsx and the Ribbon to invoke compilation and reload.