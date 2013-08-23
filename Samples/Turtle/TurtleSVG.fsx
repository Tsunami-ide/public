module Tsunami.Public.Turtle.TurtleSVG
//Inspiration from http://fssnip.net/6y

// TODO 
//    - Place SVG e.g. turtle http://wiki.laptop.org/images/2/23/Activity-turtleart.svg
//      SVG Transforms... skew, scale, transform, rotate... arbitrary... (Point -> Point)
// TODO - WPF version with animation
// TODO - Finish Arcs

#r "System.Xml.Linq"
#r "WindowsBase.dll"
open System.Xml.Linq
open System
open System.Windows

[<AutoOpen>]
module SVG =
    let svg = XNamespace.Get "http://www.w3.org/2000/svg"
    let doctype = XDocumentType("svg","Public","-//W3C//DTD SVG 1.1//EN",null)
    let attrs (xs:(string*string) seq) = [| for (name,value) in xs -> XAttribute(XName.Get name, value) |]

    [<Flags>]
    type SVGArcFlag =
        | LargeArc = 1
        | Sweep = 2

    type SVGPath2D =
        | M of float*float
        | L of float*float
        /// C1, C2, final pos
        | C of ((float*float)*(float*float)*(float*float))
        // XRadius YRadius Rotation Flag
        | A of float*float*float*SVGArcFlag*(float*float)
        | Z

    type Path = {
        /// Note: Data stored in reverse
        data : SVGPath2D list
        fill : string option
        stroke : string option
        strokeWidth : float
    } 
    with
        member this.Append(x:SVGPath2D) = {this with data = x::this.data}
        member this.ToXElement() =
                XElement(svg + "path", 
                    XAttribute(XName.Get "d", 
                        [ 
                            for d in this.data |> List.rev -> 
                                match d with
                                | M(x,y) -> sprintf "M %g %g" x y
                                | L(x,y) -> sprintf "L %g %g" x y
                                | C((x,y),(cx1,cy1),(cx2,cy2)) -> sprintf "C %g,%g %g,%g %g,%g" cx1 cy1 cx2 cy2 x y
                                | A(radX,radY,rotateX,flags,(x,y)) ->
                                    let largeArc = if flags.HasFlag SVGArcFlag.LargeArc then 1 else 0
                                    let sweep = if flags.HasFlag SVGArcFlag.Sweep then 1 else 0
                                    sprintf "A %g %g %g %i %i %g %g" radX radY rotateX largeArc sweep x y
                                | Z -> "Z"
                        ]
                        |> String.concat " "
                        ),
                    [ 
                        yield ("stroke-width",string this.strokeWidth)
                        yield ("fill",match this.fill with | Some(fill) -> fill | _ -> "none")
                        match this.stroke with | Some(stroke) -> yield ("stroke",stroke) | _ -> ()
                    ] |> attrs)

    
    type SVGText = {
        x : float
        y : float
        text : string
        fontFamily : string
        fontSize : float
        fill : string
    } 
    with
        member this.ToXElement() =
            XElement(svg + "text", 
                [
                    ("x",string this.x)
                    ("y",string this.y)
                    ("font-family",this.fontFamily)
                    ("font-size",string this.fontSize)
                    ("fill",string this.fill)
                ] |> attrs,
                this.text)
        static member Default =
            {
                x = 0.
                y = 0.
                text = ""
                fontFamily = "Verdana"
                fontSize = 20.
                fill = "blue"
            }

    type SVGRect = {
        rect : Rect
        fill : string option
        stroke : string option
        strokeWidth : float
    } 
    with 
        member this.ToXElement() =
                XElement(svg + "rect", 
                    [
                        ("x",string this.rect.X)
                        ("y",string this.rect.Y)
                        ("height",string this.rect.Height)
                        ("width",string this.rect.Width)
                    ] |> attrs,
                    [ 
                        yield ("stroke-width",string this.strokeWidth)
                        yield ("fill",match this.fill with | Some(fill) -> fill | _ -> "none")
                        match this.stroke with | Some(stroke) -> yield ("stroke",stroke) | _ -> ()
                    ] |> attrs)
    
    type Line = {
        p1 : Point
        p2 : Point
        stroke : string
        strokeWidth : float
    }
    with 
        member this.ToXElement() =
                XElement(svg + "line", 
                    [
                        ("x1",string this.p1.X)
                        ("y1",string this.p1.Y)
                        ("x2",string this.p2.X)
                        ("y2",string this.p2.Y)
                        ("stroke", this.stroke)
                        ("stroke-width",string this.strokeWidth)
                    ] |> attrs)

    type SVGObj =
        | SPath of Path
        | SRect of SVGRect
        | SLine of Line
        | SText of SVGText
        with member this.ToXElement() =
                match this with 
                | SPath(x) -> x.ToXElement()
                | SRect(x) -> x.ToXElement()
                | SLine(x) -> x.ToXElement()
                | SText(x) -> x.ToXElement()

    type SVG = {
        title : string option
        description : string option
        width : string
        height : string
        objs : SVGObj list
        viewBox : Rect
    } 
    with 
        member this.ToXElement() =
                XElement(svg + "svg", 
                     [
                        ("width",this.width)
                        ("height",this.height)
                        ("viewBox", sprintf "%g %g %g %g" this.viewBox.X this.viewBox.Y this.viewBox.Width this.viewBox.Height)
                    ] |> attrs,
                    [
                        match this.title with | Some(title) -> yield XElement(svg + "title", title) | _ -> ()
                        match this.description with | Some(desc) -> yield XElement(svg + "desc", desc) | _ -> ()
                    ],
                    this.objs |> Seq.map (fun x -> x.ToXElement())
                )

[<AutoOpen>]
module AST =    
    type instruction =
        | Move of float
        | Turn of float
        | PenDown 
        | PenUp
        | PenColor of string
        | PenWidth of float
        | Close
        | Fill of string option
        /// (radX,radY,rotateX,flags,(x,y))
        | Arc of float*float*float*SVGArcFlag*(float*float)
        /// dist, (dist1,angle1)(dist2,angle2)
        | Curveto of (float*(float*float)*(float*float))
        
        | PlaceText of string
        // TODO Speed
        // TODO Place SVG...

[<AutoOpen>]
module Lang =
    let move n = Move n
    let turn n = Turn n
    let color c = PenColor c
    let up = PenUp
    let down = PenDown
    let width n = PenWidth n
    let fillNone = Fill(None)
    let fill c = Fill(Some(c))
    let close = Close
    let curveto x = Curveto x
    let arc x = Arc x
    let text x = PlaceText x

[<AutoOpen>]
module VM = 
    let private moveWithAngle (dist:float) (angle:float) (p:Point) =
        let rad = (Math.PI * (angle - 180.)) / 180. 
        Point(Math.Round(p.X + dist * sin rad,5), Math.Round(p.Y + dist * cos rad,5) )

    type turtle =
        { 
            position : Point
            /// 0° is straight up
            angle : float
            color : string
            strokeWidth : float
            draw : bool
            activePath : Path option
            previousSVGObjs : SVGObj list
            fill : string option
        }
    
    let rec newPath (f:turtle -> turtle) (t:turtle) = (execute up t |> f) |> execute down 
    and  execute (x:instruction) (t:turtle) =
            match x with
            | Move(dist) -> 
                let pos = moveWithAngle dist t.angle t.position
                {t with 
                    position = pos
                    activePath = 
                        match t.activePath with
                        | Some(path) -> Some(path.Append(if t.draw then SVGPath2D.L(pos.X,pos.Y) else SVGPath2D.M(pos.X,pos.Y)))
                        | _ -> t.activePath
                }
            | Curveto(dist,(move1,angle1),(move2,angle2)) -> 
                let pos = moveWithAngle dist t.angle t.position
                let c1 = moveWithAngle move1 (t.angle + angle1) t.position
                let c2 = moveWithAngle move2 (t.angle + angle2) pos
                let f (p:Point) = (p.X,p.Y)
                {t with 
                    position = pos
                    activePath = 
                        match t.activePath with
                        | Some(path) -> Some(path.Append(if t.draw then SVGPath2D.C(f(pos),f(c1),f(c2)) else SVGPath2D.M(pos.X,pos.Y)))
                        | _ -> t.activePath
                }
            | Turn(angle) -> {t with angle = t.angle - angle}
            | PenDown -> 
                if t.draw = true
                then t // no-op
                else
                    {t with 
                        draw = true
                        activePath = 
                            {
                                data = [M(t.position.X,t.position.Y)]
                                fill = t.fill
                                stroke = Some(t.color)
                                strokeWidth = t.strokeWidth
                            } |> Some
                        }
            | PenUp -> 
                if t.draw = false 
                then t // no-op
                else 
                    {t with 
                        draw = false
                        activePath = None
                        previousSVGObjs = match t.activePath with | Some(x) -> SPath(x)::t.previousSVGObjs | None -> t.previousSVGObjs
                        }
            | Arc(_) -> failwith "todo"
            | PenColor(c) -> 
                if t.color = c 
                then t // no-op
                else 
                    if t.draw 
                    then newPath(fun x -> {x with color = c}) t
                    else {t with color = c}
            | PenWidth(width) ->  
                if t.strokeWidth = width 
                then t // no-op
                else newPath(fun x -> {x with strokeWidth = width}) t
                    
            | Close -> {t with activePath = (match t.activePath with | Some(path) -> path.Append(SVGPath2D.Z) |> Some | _ -> None)}
            | Fill(fill)-> 
                if t.fill = fill 
                then t // no-op
                else newPath(fun x -> {x with fill = fill}) t
            | PlaceText(text) -> {t with previousSVGObjs = SText({SVGText.Default with x=t.position.X; y=t.position.Y; text = text})::t.previousSVGObjs}
                
            
    

    let run (t:turtle) (xs:instruction list) = xs |> List.fold (fun turtle instruction -> execute instruction turtle) t

    let toSVG (showTurtle:bool) (t:turtle) =
            let t' = 
                if showTurtle 
                then 
                    [
                     color "green";
                     up; move 5.; down; 
                     turn 150.;  move 10.; 
                     turn 120.; move 10.; 
                     turn 120.; move 10.; 
                     turn 150.; up; move 5.; 
                     turn 180.; down
                     ]
                     |> run t
                    
                else execute up t
                
            { 
                    title = None
                    description = None
                    width = "800px"
                    height = "800px"
                    viewBox = Rect(0.,0.,800.,800.)
                    objs = t'.previousSVGObjs |> List.rev

            }.ToXElement()

    let toHTML (showTurtle:bool) (t:turtle) =
        let doctype = XDocumentType("html",null,null,null)
        let compatibilityMode = XElement(XName.Get "meta", attrs[("http-equiv", "X-UA-Compatible");("content", "IE=9")])
        let charset = XElement(XName.Get "meta", attrs([("charset", "utf-8")]))
        let svg = toSVG showTurtle t
        let head = [compatibilityMode]
        let body = [svg]
        let doc = XDocument(doctype,XElement(XName.Get "html", XElement(XName.Get "head",charset, head), XElement(XName.Get "body", body)))
        doc.ToString(SaveOptions.OmitDuplicateNamespaces)

    let runDefault (xs:instruction list) = [yield width 3.; yield color "red"; yield down; yield! xs] |> run { position = Point(400.,400.); angle = 0.; fill = None; draw = true; color = "black"; strokeWidth = 3.; activePath = None; previousSVGObjs = [] }

[<AutoOpen>]
module Colors =
    let red = "red"
    let green = "green"
    let blue = "blue"
    let white = "white"
    let black = "black"
    let gray = "gray"
    let yellow = "yellow"
    let orange = "orange"
    let brown = "brown"
    let cyan = "cyan"
    let magenta = "magenta"
    let purple = "purple"
    let colors = [|red;green;blue;white;black;gray;yellow;orange;cyan;magenta;purple|]
    let private rand = System.Random()
    let getRandomColor() = 
        colors.[rand.Next(colors.Length - 1)]