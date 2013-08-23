module Tsunami.Public.Games.MissileCommand
//http://trelford.com/blog/post/MissileCommand.aspx

#r "WindowsBase.dll"
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "System.Xaml.dll"
#r "Tsunami.IDEDesktop.exe"
#r "UIAutomationTypes.dll"

open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Shapes
open System.Windows.Threading

[<AutoOpen>]
module Resources =
    let rand  = System.Random()
    let toPoints (xys) = 
        let coll = PointCollection()
        xys |> Seq.iter (fun (x,y) -> coll.Add(Point(x,y)))
        coll
    let toGradientStops stops =
            let collection = GradientStopCollection()
            stops
            |> List.map (fun (color,offset) -> GradientStop(Color=color,Offset=offset)) 
            |> List.iter collection.Add
            collection

    let skyBrush =
        lazy 
            let darkBlue = Color.FromArgb(255uy,0uy,0uy,40uy)
            let stops = [Colors.Black,0.0; darkBlue,1.0] |> toGradientStops
            LinearGradientBrush(stops, 90.0)

    let sandBrush = lazy(SolidColorBrush(Colors.Yellow))
    let downBrush = lazy(LinearGradientBrush([Colors.Black,0.0;Colors.White,1.0] |> toGradientStops, 90.0))
    let upBrush = lazy(LinearGradientBrush([Colors.White,0.0;Colors.Black,1.0] |> toGradientStops, 90.0))
    let missileBrush = lazy(SolidColorBrush(Colors.Red) :> Brush)
    let bombBrush = lazy(RadialGradientBrush(Colors.Yellow,Colors.White) :> Brush)

type Missile (x,y,isBomb) =
    let canvas = Canvas ()
    let brush = if isBomb then downBrush else upBrush
    let line = Line(X1=x, Y1=y, X2=x, Y2=y, Stroke=brush.Force())
    let endPoint = TranslateTransform()
    let circle = Ellipse(Width=3.0,Height=3.0,Fill=missileBrush.Force(),RenderTransform=endPoint)
    do  canvas.Children.Add line |> ignore
        canvas.Children.Add circle  |> ignore
    member this.IsBomb = isBomb
    member this.Control = canvas
    member this.Update(x,y) =
        line.X2 <- x
        line.Y2 <- y
        endPoint.X <- x - 1.5
        endPoint.Y <- y - 1.5
    static member Path (x1,y1,x2,y2,velocity) = seq {
        let x, y = ref x1, ref y1
        let dx,dy = x2 - x1, y2 - y1
        let angle = atan2 dx dy
        let length = sqrt(dx * dx + dy * dy)
        let steps = length/velocity
        for i = 1 to int steps do
            y := !y + cos(angle)*velocity
            x := !x + sin(angle)*velocity
            yield !x , !y
        }

type Explosion (x,y) =
    let explosion = Ellipse(Opacity=0.5, Fill=bombBrush.Force())
    member this.Control = explosion
    member this.Update r =
        explosion.RenderTransform <- 
            TranslateTransform(X = x - r, Y = y - r)
        explosion.Width <- r * 2.0
        explosion.Height <- r * 2.0 
    static member Path radius = seq {
        for i in [50..2..100] do
            yield radius * ((float i / 100.0) ** 3.0)
        for i in [100..-1..0] do
            yield radius * ((float i / 100.0) ** 3.0)
        }

type City (x,y,width,height) =
    let canvas = Canvas ()
    let fill ws hs brush =
        let mutable i = 0
        do while i < width do
            let w = Seq.nth (Seq.length ws |> rand.Next) ws 
            let h = Seq.nth (Seq.length hs |> rand.Next) hs 
            Rectangle(Width=float w,Height=float h, Fill=brush,
                RenderTransform=TranslateTransform(X=x+float i,Y=y+float (height-h)))
            |> canvas.Children.Add
            |> ignore
            i <- i + w
    do  SolidColorBrush Colors.Blue |> fill [2..4] [height/2..height] 
    do  SolidColorBrush Colors.Cyan |> fill [1..3] [height/4..height*2/3] 
    member this.Control = canvas
    member this.IsHit (x',y') =
        x' >= x && x' < x + float width &&
        y' >= y && y' < y + float height

type GameControl () as this =
    inherit UserControl ()

    let mutable disposables = []
    let remember disposable = disposables <- disposable :: disposables
    let dispose (d:IDisposable) = d.Dispose()
    let forget () = disposables |> List.iter dispose; disposables <- []

    let width, height = 500.0, 500.0
    do  this.Width <- width; this.Height <- height
    let canvas = Canvas(Background=skyBrush.Force(), Cursor=Cursors.Cross)
    let add (x:#UIElement) = canvas.Children.Add x
    let remove (x:#UIElement) = canvas.Children.Remove x |> ignore

    let planet = Rectangle(Width=width, Height=20.0, Fill=sandBrush.Force())
    do  planet.RenderTransform <- TranslateTransform(X=0.0,Y=height-20.0)
    do  add planet |> ignore
    let platform = System.Windows.Shapes.Polygon(Fill=sandBrush.Force())
    do  platform.Points <- 
            let center = width/2.0
            [center,height-40.0;center-40.0,height;center+40.0,height]
            |> toPoints
    do  add platform |> ignore

    let mutable score = 0
    let mutable cities = []
    let mutable missiles = []
    let mutable explosions = []
    let mutable wave = 5

    let scoreControl = TextBlock(Foreground=SolidColorBrush Colors.White)
    do  scoreControl.Text <- sprintf "SCORE %d" score
    do  add scoreControl |> ignore

    do  cities <- [1..4] |> List.map (fun i -> City((width*(float i)/5.0)-25.0,height-33.3,40,15))
        cities |> List.iter (fun city -> add city.Control |> ignore)

    let fireMissile (x1,y1,x2,y2,velocity,isBomb) =       
        let missile = Missile(x1,y1,isBomb)
        let path = Missile.Path(x1,y1,x2,y2,velocity)
        missiles <- ((x2,y2),missile,path.GetEnumerator()) :: missiles
        add missile.Control

    let startExplosion (x,y) = 
        let explosion = Explosion(x,y) 
        let path = Explosion.Path 50.0
        explosions <- ((x,y),explosion,path.GetEnumerator()) :: explosions
        explosion.Control |> add

    let dropBombs count =
        for i = 1 to count do 
        let x1, x2 = rand.NextDouble()*width, rand.NextDouble()*width
        fireMissile(x1,0.0,x2,height-20.0,1.0,true) |> ignore

    let update () =
        let current, expired = 
            explosions |> List.partition (fun (_,_,path) -> path.MoveNext())
        explosions <- current
        expired |> List.iter (fun (_,explosion:Explosion,_) -> remove explosion.Control)
        current |> List.iter (fun (_,explosion,path) -> path.Current |> explosion.Update)

        let current, expired = missiles |> List.partition (fun (_,_,path) -> path.MoveNext())
        expired |> List.iter (fun (target,missile:Missile,path) -> 
            remove missile.Control
            startExplosion target |> ignore
        )
        current |> List.iter (fun (_,missile:Missile,path) -> path.Current |> missile.Update)
        let hit,notHit,casualties = 
            current 
            |> List.fold  (fun (hit,notHit,casualties) missile ->
                let _,_, path = missile
                let x,y = path.Current
                let isHit = 
                    explosions |> List.exists (fun ((x',y'),_,path) ->
                        let r = path.Current
                        (x - x') ** 2.0 + (y - y') ** 2.0 < (r**2.0) 
                    )
                let casualty = cities |> List.tryFind (fun city -> city.IsHit(x,y))
            
                let casualties = 
                    match casualty with
                    | Some city -> city::casualties
                    | None -> casualties

                match isHit || Option.isSome casualty with
                | true -> (missile::hit,notHit,casualties)
                | false -> (hit,missile::notHit,casualties)          
            ) ([],[],[])
        hit |> List.iter (fun (_,missile,path) ->
            let x,y = path.Current
            startExplosion(x,y) |> ignore
            remove missile.Control
        )
        missiles <- notHit
        score <- score + 10 * (hit |> List.filter (fun (_,missile,_) -> missile.IsBomb) |> List.length)
        casualties |> List.iter (fun city -> remove city.Control)
        cities <- cities |> List.filter (fun city -> casualties |> List.exists ((=) city) |> not)
        scoreControl.Text <- sprintf "SCORE %d" score

        if missiles.Length = 0 then
            wave <- wave + 1
            dropBombs wave

        cities.Length > 0

    let message s =
        let t = TextBlock(Text=s)
        t.HorizontalAlignment <- HorizontalAlignment.Center
        t.VerticalAlignment <- VerticalAlignment.Center
        t.Foreground <- SolidColorBrush Colors.White
        t

    let layout = Grid()

    let startGame () =
        dropBombs wave

        canvas.MouseLeftButtonDown
        |> Observable.subscribe (fun me ->
            let point = me.GetPosition(canvas)
            fireMissile(width/2.0,height-40.0,point.X,point.Y,2.0,false) |> ignore
        )
        |> remember

        let timer = DispatcherTimer()
        timer.Interval <- TimeSpan.FromMilliseconds(20.0)
        timer.Tick
        |> Observable.subscribe (fun _ -> 
            let undecided = update ()
            if not undecided then
                message "The End" |> layout.Children.Add |> ignore
                forget()
        )
        |> remember
        timer.Start()
        {new IDisposable with member this.Dispose() = timer.Stop()}
        |> remember
    
    do  layout.Children.Add canvas |> ignore
        this.Content <- layout
    
    do  let t = message "Click to Start"
        layout.Children.Add t |> ignore
        this.MouseLeftButtonUp
        |> Observable.subscribe (fun _ -> 
            forget ();
            layout.Children.Remove t |> ignore
            startGame() )
        |> remember

    interface System.IDisposable with
        member this.Dispose() = forget()

Tsunami.IDE.SimpleUI.addControlToNewDocument("Tetris",fun _ -> new GameControl() :> UIElement)