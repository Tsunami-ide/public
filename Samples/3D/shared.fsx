(* NOTE: Run in a seperate FSI Shell *)
#r "PresentationCore"
#r "PresentationFramework"
#r "WindowsBase"
#r "System.Xaml"
#r "UIAutomationTypes"

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text
open System.Windows
open System.Windows.Shapes
open System.Windows.Controls
open System.Windows.Markup
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Media3D
open System.Windows.Media.Imaging
open System.Windows.Threading
open System.Xml
open System.Diagnostics
open System.Threading
open System.Xaml


module Vector3D =
    let cross (v1 : Vector3D) (v2 : Vector3D) = Vector3D.CrossProduct(v1,v2)
    let dot (v1 : Vector3D) (v2 : Vector3D) = Vector3D.DotProduct(v1,v2)
    let length (v : Vector3D) = v.Length
    let lengthSquared (v : Vector3D) = v.LengthSquared
    let angle_between (v1 : Vector3D) (v2 : Vector3D) = acos ((dot v1 v2) / ((v1 |> length) * (v2 |> length)))
    let unitV = Vector3D(1.,1.,1.)
    let XAxis = Vector3D(1.,0.,0.)
    let YAxis = Vector3D(0.,1.,0.)
    let ZAxis = Vector3D(0.,0.,1.)

module Matrix3D =
    open Vector3D
    let unitM = Matrix3D.Identity
    let scale(v:Vector3D) = 
            let mutable m = Matrix3D.Identity
            m.Scale(Vector3D(v.X,v.Y,v.Z))
            m

    let translate(v:Vector3D) = 
            let mutable m = Matrix3D.Identity
            m.Translate(Vector3D(v.X,v.Y,v.Z))
            m
    
    let rotate(x:Quaternion) =
        let mutable m = Matrix3D.Identity
        m.Rotate(x)
        m
        
(* Helper functions *)
let parseXAML (xaml : string) =
    use ms = new MemoryStream(Encoding.ASCII.GetBytes(xaml))
    ms.Position <- int64 0
    XamlReader.Load(ms)
    
let getUrlAsTxt (url:string) =
    use sr = new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream())
    sr.ReadToEnd()
      
            
let square() = 
    let mg = MeshGeometry3D()
    [|-1.,1.,0.;1.,1.,0.;-1.,-1.,0.;1.,-1.,0.|] 
    |> Array.iter (fun (x,y,z) -> mg.Positions.Add(Point3D(x,y,z)))
    [|0.,0.,1.;0.,0.,1.;0.,0.,1.;0.,0.,1.|]
    |> Array.iter (fun (x,y,z) -> mg.Normals.Add(Vector3D(x,y,z)))
    [|0.,0.;1.,0.;0.,1.;1.,1.|] 
    |> Array.iter (fun (x,y) -> mg.TextureCoordinates.Add(Point(x,y)))
    [|0;2;3;0;3;1|] |> Array.iter mg.TriangleIndices.Add
    mg
    
let cube() = 
    let mg = MeshGeometry3D()
    [|
        -0.5, 1.0, 0.5; 0.5, 1.0, 0.5
        -0.5, 0.0, 0.5; 0.5, 0.0, 0.5

        0.5 , 1.0,-0.5;-0.5, 1.0,-0.5
        0.5 , 0.0,-0.5;-0.5, 0.0,-0.5

        -0.5, 1.0,-0.5;-0.5, 1.0, 0.5
        -0.5, 0.0,-0.5;-0.5, 0.0, 0.5

        0.5 , 1.0, 0.5; 0.5, 1.0,-0.5
        0.5 , 0.0, 0.5; 0.5, 0.0,-0.5

        -0.5, 1.0,-0.5; 0.5, 1.0,-0.5
        -0.5, 1.0, 0.5; 0.5, 1.0, 0.5

        0.5 , 0.0,-0.5;-0.5, 0.0,-0.5
        0.5 , 0.0, 0.5;-0.5, 0.0, 0.5
    |] |> Array.iter (fun (x,y,z) -> mg.Positions.Add(Point3D(x,y,z)))

    [|
        0 , 2, 1; 1, 2, 3
        4 , 6, 5; 5, 6, 7
        8 ,10, 9; 9,10,11
        12,14,13;13,14,15
        16,18,17;17,18,19
        20,22,21;21,22,23
    |] 
    |> Array.collect (fun (a,b,c) -> [|a;b;c|]) 
    |>  Array.iter mg.TriangleIndices.Add

    [|0., 0.; 1., 0.; 0., 1.; 1., 1.|] 
    |> Array.create 6
    |> Array.collect id
    |> Array.iter (fun (x,y) -> mg.TextureCoordinates.Add(Point(x,y)))
    mg
         
type Viewer() as this  = 
    inherit Window()
    
    let grid = Grid()
    let viewport = Viewport3D()
    let backPanel = Canvas(Background = Brushes.DarkBlue, IsHitTestVisible = true)
    let frontPanel = Canvas(IsHitTestVisible = false)

    do
        
        

        grid.Children.Add(backPanel) |> ignore
        grid.Children.Add(viewport) |> ignore
        grid.Children.Add(frontPanel) |> ignore
        this.Content <- grid

            // Add camera to viewport
        let camera = PerspectiveCamera(Point3D(0.,0.,0.), Vector3D(0., 0., 1.), Vector3D(0., 1., 0.), 45.)
        viewport.Camera <- camera
        // Create the transforms
        let zoom = TranslateTransform3D()
        let tran = TranslateTransform3D()
        let rotx = AxisAngleRotation3D(Vector3D(1.,0.,0.),0.)
        let roty = AxisAngleRotation3D(Vector3D(0.,1.,0.),0.)
        let rotz = AxisAngleRotation3D(Vector3D(0.,0.,1.),0.)
    
        zoom.OffsetZ <- -10.
        roty.Angle <- 180.
    
         // Add the transform to the camera
        let group = Transform3DGroup()
        group.Children.Add(zoom)
        group.Children.Add(RotateTransform3D(rotz))
        group.Children.Add(RotateTransform3D(rotx))
        group.Children.Add(RotateTransform3D(roty))
        group.Children.Add(tran)
        camera.Transform <- group 
    
        let addFront c = frontPanel.Children.Add(c)

        let removeFront c = frontPanel.Children.Remove(c)
    
        let getAspectRatio() = viewport.ActualWidth / viewport.ActualHeight
    
        let getProjMatrix (camera:PerspectiveCamera) aspectRatio =
            let degToRad deg = deg * (Math.PI / 180.0)
            let hFov = degToRad camera.FieldOfView
            let zn = camera.NearPlaneDistance
            let zf = camera.FarPlaneDistance
            let xScale = 1.0 / tan(hFov / 2.0)
            let yScale = aspectRatio * xScale
            let m33 = 
                if zf = Double.PositiveInfinity then 
                    -1.
                else
                    zf / (zn - zf)
            let m43 = zn * m33
            Matrix3D(
                        xScale,      0.,   0.,   0.,
                            0.,  yScale,   0.,   0.,
                            0.,      0.,  m33,  -1.,
                            0.,      0.,  m43,   0.)
                        
        let getViewMatrix (camera:PerspectiveCamera) =
            let zAxis = -camera.LookDirection
            zAxis.Normalize()
            let xAxis = Vector3D.CrossProduct(camera.UpDirection, zAxis)
            xAxis.Normalize()
            let yAxis = Vector3D.CrossProduct(zAxis,xAxis)
            let position = Vector3D(X=camera.Position.X,Y=camera.Position.Y,Z=camera.Position.Z)
            let offsetX = -Vector3D.DotProduct(xAxis,position)
            let offsetY = -Vector3D.DotProduct(yAxis,position)
            let offsetZ = -Vector3D.DotProduct(zAxis,position)
            Matrix3D(
                    xAxis.X, yAxis.X, zAxis.X, 0.,
                    xAxis.Y, yAxis.Y, zAxis.Y, 0.,
                    xAxis.Z, yAxis.Z, zAxis.Z, 0.,
                    offsetX, offsetY, offsetZ, 1.)

    
        // Camrea Settings
        let first = ref true // set to true when viewport3D focus is lost
        let lastMouseMovePos = ref (Point())
        let lastMouseDownPos = ref (Point())

        let mouse_move = fun (e:Input.MouseEventArgs) ->
            let p = e.GetPosition(viewport)
            if !first then
                    lastMouseMovePos := p
                    first := false
            let d = !lastMouseMovePos - p
            if e.RightButton = Input.MouseButtonState.Pressed then
                if e.LeftButton = Input.MouseButtonState.Pressed then
                    // zoom
                    zoom.OffsetZ <- zoom.OffsetZ + zoom.OffsetZ * 10. * d.Y / viewport.ActualHeight
                else
                    // rotation
                    rotx.Angle <- rotx.Angle + (d.Y / viewport.ActualHeight) * 180. // z is pitch
                    roty.Angle <- roty.Angle + (d.X / viewport.ActualWidth) * 180.             
            lastMouseMovePos := p  

        backPanel.MouseMove.Add(mouse_move)
        viewport.MouseMove.Add(mouse_move)
            
        backPanel.MouseWheel.Add(fun e -> if e.Delta < 0 then zoom.OffsetZ <- zoom.OffsetZ * 1.1 else zoom.OffsetZ <- zoom.OffsetZ / 1.1)
        viewport.MouseWheel.Add(fun e -> if e.Delta < 0 then zoom.OffsetZ <- zoom.OffsetZ * 1.1 else zoom.OffsetZ <- zoom.OffsetZ / 1.1)
    
        backPanel.MouseUp.Add((fun _ -> first := true))
        viewport.MouseUp.Add((fun _ -> first := true))

        backPanel.MouseLeave.Add(fun _ -> first := true)
        viewport.MouseLeave.Add(fun _ -> first := true)
    
        let key_down = fun (e:KeyEventArgs) ->
            if Keyboard.IsKeyDown(Key.A) then tran.OffsetX <- tran.OffsetX - 1.
            if Keyboard.IsKeyDown(Key.D) then tran.OffsetX <- tran.OffsetX + 1.
            if Keyboard.IsKeyDown(Key.W) then tran.OffsetZ <- tran.OffsetZ - 1.
            if Keyboard.IsKeyDown(Key.S) then tran.OffsetZ <- tran.OffsetZ + 1.
            if Keyboard.IsKeyDown(Key.Space) then tran.OffsetY <- tran.OffsetY + 1.
            if Keyboard.IsKeyDown(Key.LeftCtrl) then tran.OffsetY <- tran.OffsetY - 1.
            e.Handled <- false
        this.KeyDown.Add(key_down)
        this.Reset()
    member this.Viewport = viewport
    member this.Reset() = 
        viewport.Children.Clear()
        let m3dgroup = Model3DGroup() 
        [|
            AmbientLight(Color = Color.FromRgb(64uy,64uy,64uy)) :> Light
            DirectionalLight(Color = Color.FromRgb(192uy,192uy,192uy), Direction = Vector3D(2.,-3.,-1.)) :> Light
        |] |> Array.iter (fun light -> m3dgroup.Children.Add(light))
        viewport.Children.Add(ModelVisual3D(Content = m3dgroup))