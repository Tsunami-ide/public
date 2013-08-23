#load "MissileCommand.fsx"
#load "Pacman.fsx"
#load "Tetris.fsx"

open Tsunami.Public.Games

let openGame(name,control) = Tsunami.IDE.SimpleUI.addControlToNewDocument(name,fun _ -> control() :> System.Windows.UIElement)

openGame("MissileCommand",fun _ -> new MissileCommand.GameControl())
openGame("Pacman",fun _ -> new Pacman.GameControl())
openGame("Tetris",fun _ -> new Tetris.GameControl())
