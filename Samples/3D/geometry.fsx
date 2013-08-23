(* NOTE: Run in a seperate FSI Shell *)
#load "shared.fsx"
#r "WindowsBase"
#r "PresentationCore"
#r "PresentationFramework"
#r "System.Windows.Presentation"
#r "System.Xaml"

open Shared
open System
open System.Collections.Generic
open System.IO
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
open Vector3D
open Matrix3D

(* Helper functions *)
module Random =
    let rand = new System.Random()
    let int n = rand.Next(n)
    let float x = x * rand.NextDouble()


(* 3D Primatives *)
module Shapes3D =

    /// Creats a two dimentional mesh from 2 parametric functions and 2 counters
    let one_d ps f1 count1 = [|for i in 0 .. count1 -> f1 i ps |]
    let two_d f1 count1 f2  count2 = [|for i in 0 .. count2 -> f2 i [|for j in 0 .. count1 -> f1 j |] |]

    /// Mesh3D Matrix Transform
    let mesh3DTransform (m:Matrix3D) (m3d:MeshGeometry3D) =
        let points = Point3DCollection()
        for i in 0 .. m3d.Positions.Count - 1 do
            points.Add(m.Transform(m3d.Positions.[i]))
        let normals = Vector3DCollection()
        for i in 0 .. m3d.Normals.Count - 1 do
            normals.Add(m.Transform(m3d.Normals.[i]))
        m3d.Positions <- points
        m3d.Normals <- normals   
        m3d

    /// Builds a mesh out of a 2d point retangular array
    let createHardMesh (mesh:Point3D array array) l w =
        let length = l + 1
        let width = w + 1
        let positions, triangles = Point3DCollection(), Int32Collection()
        
        let addTriangle a b c =
            let p = positions.Count
            positions.Add(a)
            triangles.Add(p + 2)
            positions.Add(b)
            triangles.Add(p+1)
            positions.Add(c)
            triangles.Add(p)
        
        let addSquare a b c d =
            addTriangle a b c
            addTriangle c d a
        
        for i in 0 .. width - 2 do
            for j in 0 .. length - 2 do
                addSquare mesh.[i].[j] mesh.[i].[j+1] mesh.[i+1].[j+1] mesh.[i+1].[j]
        
        MeshGeometry3D(Positions = positions,  TriangleIndices = triangles)  
    
    // Shared normals -> softer
    let createSoftMesh (mesh:Point3D array array) l w =
        let length = l + 1
        let width = w + 1
        let points = Point3DCollection()
        for i in 0 .. width - 1 do
            for j in 0 .. length - 1 do
                points.Add(mesh.[i].[j])           

        // TextureCoords - equal distribution
        let textureCoords = PointCollection()        
        for i in 0 .. width - 1 do
            for j in 0 .. length - 1 do
                textureCoords.Add(Point(float i/ float (width - 1), float j / float (length - 1)))
        
        // TriangleIndices
        let indices = Media.Int32Collection() // add 3 times for each triangle
        for i in 0 .. width - 2 do
            for j in 0 .. length - 2 do
                let a = i * length + j
                let b = i * length + j + 1
                let c = (i + 1) * length + j + 1
                let d = (i + 1) * length + j
                 // add first triangle
                indices.Add(a)
                indices.Add(b)
                indices.Add(c)
                // add second triangle
                indices.Add(a)
                indices.Add(c)
                indices.Add(d)
        // Return a new mesh geometry
        MeshGeometry3D(Positions = points, TextureCoordinates = textureCoords, TriangleIndices = indices)  
                 
    /// cone, horizontal count, rotational count
    let cylinder h r = 
        let f1 = fun i -> 
            let x = float i / float h
            Point3D(x,0.5,0.) // straight line
        let f2 = fun i ps -> 
            let m = rotate (Quaternion(XAxis, ((float i / float r) * 360.)))
            ps |> Array.map (fun (p:Point3D) -> m.Transform(p))
        let mesh = createSoftMesh (two_d f1 h f2 r) h r
        let normals = Vector3DCollection()
        for p in mesh.Positions do
            normals.Add(Point3D(p.X,0.,0.) - p)
        mesh.Normals <- normals
        mesh
    
    let cone h r = 
        let f1 = fun i -> 
            let x = float i / float h
            Point3D(x,1.0 - x,0.) // decending line
        let f2 = fun i ps -> 
            let m = rotate (Quaternion(XAxis, ((float i / float r) * 360.)))
            ps |> Array.map (fun (p:Point3D) -> m.Transform(p))
        createSoftMesh (two_d f1 h f2 r) h r

        
    let circle h r = 
        let f1 = fun i -> 
            let x = float i / float h
            Point3D(x,0.,0.) // straight line
        let f2 = fun i ps -> 
            let m = rotate (Quaternion(YAxis, ((float i / float r) * 360.)))
            ps |> Array.map (fun (p:Point3D) -> m.Transform(p))
        createSoftMesh (two_d f1 h f2 r) h r
    
    
    /// helix
    let helix coils height tes =
        let f1 = fun i -> 
            let t = float i / float tes
            Point3D(cos(2.*Math.PI* coils * t),sin(2.*Math.PI* coils * t),t * height) // straight line
        let f2 = fun i ps -> 
            if i = 0 then
                ps
            else
                ps |> Array.map (fun (p:Point3D) -> Point3D(0.,0.,p.Z))
        createSoftMesh (two_d f1 tes f2 2) tes 2
            
            
    /// Square with one texture for all 6 sides
    let sphere t =
        // rotate a semicircle around the x axis
        let f1 = fun i -> 
            let x = float i / float t - 0.5
            let theta = x * Math.PI
            Point3D(sin(theta), cos(theta),0.0)
            
        let f2 = fun i ps -> 
            let m = rotate  (Quaternion(XAxis,((float i / float t) * 360.))) 
            ps |> Array.map (fun (p:Point3D) -> m.Transform(p))        
        let mesh = createSoftMesh (two_d f1 t f2 t) t t
        // manually specify normals
        let normals = Vector3DCollection()
        let c = Point3D(0.,0.,0.)
        for p in mesh.Positions do
            normals.Add(p - c)
        mesh.Normals <- normals
        mesh

    let inverse_sphere() t =
        // rotate a semicircle around the x axis
        let f1 = fun i -> 
            let x = float i / float t - 0.5
            let theta = x * Math.PI
            Point3D(sin(theta), cos(theta),0.0)
            
        let f2 = fun i ps -> 
            let m = rotate  (Quaternion(XAxis,((float i / float t) * 360. * -1.))) 
            ps |> Array.map (fun (p:Point3D) -> m.Transform(p))
         
        let mesh = createSoftMesh (two_d f1 t f2 t) t t
        // manually specify normals
        let normals = Vector3DCollection()
        let c = Point3D(0.,0.,0.)
        for p in mesh.Positions do
            normals.Add(c - p)
        mesh.Normals <- normals
        mesh

module Geometry3D =
    /// merge geometries
    let private mergeGeom (g1:MeshGeometry3D) (g2:MeshGeometry3D) =
        // create a new geometry with all the triangles/positions
        let positions, textures, triangles, normals = Point3DCollection(), PointCollection(), Int32Collection(), Vector3DCollection()
        // add g1
        for p in g1.Positions do positions.Add(p)
        for t in g1.TextureCoordinates do textures.Add(t)
        for t in g1.TriangleIndices do triangles.Add(t)
        for n in g1.Normals do normals.Add(n)
        // add g2
        for p in g2.Positions do positions.Add(p)
        for t in g2.TextureCoordinates do textures.Add(t)
        for t in g2.TriangleIndices do triangles.Add(t + g1.Positions.Count)
        for n in g2.Normals do normals.Add(n)
        // should eliminate non visible triangles
        MeshGeometry3D(Positions = positions, TextureCoordinates = textures, TriangleIndices = triangles, Normals = normals) 

    /// concave fill - clockwise (asumption) points depicting a shape
    let private convexFill (ps:Point3D array) =
        // setup positions, triangles, normals
        let positions, triangles = Point3DCollection(), Int32Collection()
        // Add the points to the array
        ps |> Array.iter (fun p -> 
                positions.Add(p)
            )
        // Add the triangles
        for i in 0 .. ps.Length - 1 do
            triangles.Add(0)
            triangles.Add(i)
            triangles.Add(i + 1)
        MeshGeometry3D(Positions = positions, TriangleIndices = triangles) 
    
    /// convert the convex array of points into an array of concave points
    let private concaveFill(psa:Point3D array) = 
        let ps = Array.toList psa
        let positions, triangles = Point3DCollection(), Int32Collection()
        // function for finding out the direction of the turn
        let turnRight a b c = (Vector3D.CrossProduct(b - a, c - a)).Z < 0.
        // function for finding out if a point is in the triangle abc 
        let dinabc (a:Point3D) (b:Point3D) (c:Point3D) (d:Point3D) = // false
            let dot a b c = Vector3D.DotProduct(b-a,c-a)
            let acute a b c = (dot a b c) < 0.
            // point maps to inside the triangle if
            (acute a b d) && (acute b c d) && (acute c a d) && (acute a c d) && (acute c b d) && (acute b a d)

        let right_left = ref 0
        for i in 0 .. psa.Length - 3 do
            let a,b,c = psa.[i],psa.[i+1],psa.[i+2]
            if turnRight a b c then 
                    right_left := !right_left + 1 
                else 
                    right_left := !right_left - 1
                    
        let turnCorrect a b c =
            if !right_left > 0 
                then turnRight a b c 
                else turnRight a b c |> not
             
        let tc = ref 0 // traingle counter
        let skipCount = ref 0// the number of triangles skipped, if skipCount = list.Count then reverse list and try again
        let rec getTriangles (ps:Point3D list) =
            match ps with
            | [a;b;c] -> // Add the triangle
                 positions.Add(a)
                 triangles.Add(!tc*3)
                 positions.Add(b)
                 triangles.Add(!tc*3 + 1)
                 positions.Add(c)
                 triangles.Add(!tc*3 + 2)
                 tc := !tc + 1
            | a::b::c::tail -> 
                if turnCorrect a b c then
                    if List.exists (fun d -> dinabc a b c d) tail then
                        // there exists a point in this triangle, don't remove, move on
                        getTriangles (b::c::tail@[a])
                        //printf "%A"  ps
                    else
                        // found a triangle to remove
                        getTriangles [a;b;c]
                        // continune without b
                        getTriangles (a::c::tail)
                else
                    // move on
                    if !skipCount = tail.Length + 3 then
                        List.rev (b::c::tail@[a]) |> getTriangles
                    else
                        skipCount := !skipCount + 1
                        getTriangles (b::c::tail@[a])
            | [] -> ()
            | [_] -> ()
            | [_;_] -> ()
            
        getTriangles ps
        MeshGeometry3D(Positions = positions, TriangleIndices = triangles) 

    type Primitive3D =
    | Point of Point3D
    | Line of Point3D * Point3D
    | Shape of Point3D array
    | Geometry of MeshGeometry3D
    with
        member this.Reverse() =
            match this with
            | Point _ -> this
            | Line (p1,p2) -> (p2,p1) |> Line
            | Shape ps -> Array.rev ps |> Shape
            | Geometry g -> 
                // reverse the triangle indicies
                let triangles = Int32Collection()
                for i in 0 .. 3 .. g.TriangleIndices.Count do
                    triangles.Add(g.TriangleIndices.[i + 2])
                    triangles.Add(g.TriangleIndices.[i + 1])
                    triangles.Add(g.TriangleIndices.[i])
                g.TriangleIndices <- triangles
                g |> Geometry

        member this.Points() =
            match this with
            | Point p -> [|p|]
            | Line (p1,p2) -> [|p1;p2|]
            | Shape ps -> ps
            | Geometry g -> [| for i in 0 .. (g.Positions.Count - 1) -> g.Positions.[i] |]
           
        member this.Transform(m:Matrix3D) =
            match this with
            | Point p -> Point(m.Transform(p))
            | Line (p1,p2) -> Line(m.Transform(p1),m.Transform(p2))
            | Shape ps -> Array.map (fun (p:Point3D) -> m.Transform(p)) ps |> Shape
            | Geometry g -> 
                let positions = Point3DCollection()
                for i in 0.. g.Positions.Count - 1 do
                    positions.Add(m.Transform(g.Positions.[i]))
                g.Positions <- positions
                Geometry(g)

        member this.AsGeomteryMesh() =
                /// upgrade to a geometric object
                let rec getMesh p =
                    // width
                    let width = 0.05 // 1/20
                    match p with 
                    | Point p -> 
                        Shapes3D.sphere 30 
                        |> Shapes3D.mesh3DTransform ( (Matrix3D.translate(Vector3D(p.X,p.Y,p.Z)) * (Matrix3D.scale (Vector3D(width,width,width))) ))
                    | Line (p1,p2) -> 
                        let v = p2 - p1
                        let v1 = 
                            v.Normalize()
                            let mutable tmp = Vector3D.CrossProduct(Vector3D(1.,0.,0.), v)
                            if tmp.LengthSquared < 0.01 then 
                                tmp <- Vector3D.CrossProduct(Vector3D(0.,1.,0.), v)
                                if tmp.LengthSquared < 0.01 then
                                    tmp <- Vector3D.CrossProduct(Vector3D(0.,0.,1.), v)
                            tmp.Normalize()
                            tmp * width
                        let v2 =
                            let tmp = Vector3D.CrossProduct(v,v1)
                            tmp.Normalize()
                            tmp * width
                        let vc = -(v1 + v2) / 2.
                        // get the sphere at the first end
                        //let sph = Shapes3D.sphere() 15
                        //sph |> Shapes3D.mesh3DTransform ( (Math3D.tran_point p1.X p1.Y p1.Z) * (Math3D.scale_point width width width) |> Math3D.Matrix3D)
                        //sph |> mergeGeom 

                        Line(p1,p2)
                            .Transform(translate vc)
                            .Extrude(v2)
                            .Extrude(v1)
                            .AsGeomteryMesh()
                    | Shape ps -> concaveFill ps 
                    | Geometry g -> g
                getMesh this
        member this.AsModelVisual(material:Material) = ModelVisual3D(Content = GeometryModel3D(Geometry = this.AsGeomteryMesh(), BackMaterial = material, Material = material))
        member this.AsPoints() =
            match this with
            | Point p -> [|Point(p)|]
            | Line (p1,p2) -> [|Point(p1);Point(p2)|]
            | Shape ps -> Array.map (fun p -> Point(p)) ps
            | Geometry g -> [| for i in 0 .. (g.Positions.Count - 1)  -> Point(g.Positions.[i]) |]
        
        member this.AsLines() =
            match this with
            | Point p -> [||]
            | Line (p1,p2) -> [|Line(p1,p2)|]
            | Shape ps -> Array.append [| for i in 0 .. ps.Length - 2 -> Line(ps.[i], ps.[i+1]) |] [|Line(ps.[ps.Length-1],ps.[0])|]
            | Geometry g -> 
                [| for i in 0 .. g.TriangleIndices.Count - 2 -> 
                    Line(g.Positions.[g.TriangleIndices.[i]],g.Positions.[g.TriangleIndices.[i+1]])
                |]

        member this.AsShapes() =
            match this with
            | Point p -> [||]
            | Line (p1,p2) -> [||]
            | Shape ps -> [| Shape(ps) |]
            | Geometry g -> 
                [| for i in 0 .. 3 .. g.TriangleIndices.Count - 1 -> 
                    Shape([|g.Positions.[g.TriangleIndices.[i+1]];g.Positions.[g.TriangleIndices.[i+2]];g.Positions.[g.TriangleIndices.[i]]|])
                |]

        member this.Extrude(v:Vector3D) : Primitive3D =
            match this with
            | Point p -> Line(p, (Matrix3D.translate(v).Transform(p)))
            | Line (p1,p2) -> 
                let m = Matrix3D.translate(v)
                Shape([| p1; p2; m.Transform(p2); m.Transform(p1) |])
            | Shape ps ->  
                 let m = Matrix3D.translate(v)
                 // Add the first point onto the end
                 let ps1 = Array.append ps [|ps.[0]|]
                 let pss = Shapes3D.one_d ps1 (fun i ps1 -> if i = 0 then ps1 else ps1 |> Array.map (fun p -> m.Transform(p))) 1
                 let sides = Shapes3D.createHardMesh pss (ps1.Length - 1) 1
                 let front = ps1 |> concaveFill
                 let back = Array.rev (Array.map (fun (p:Point3D) -> m.Transform(p)) ps1 ) |> concaveFill
                 mergeGeom front back |> mergeGeom sides |> Geometry
            | Geometry g -> Geometry(g)

        member this.Revolve(axis:Vector3D, angle:float, tes:int) =
            let rev_p (p:Point3D) = [|for i in 0 .. tes ->  (Matrix3D.rotate(Quaternion(axis, angle * (float i / float tes)))).Transform(p)|]
            match this with
            | Point p -> rev_p p |> Shape
            | Line (p1,p2) -> 
                let top = rev_p p1
                let bottom = rev_p p2
                (Array.append top (Array.rev bottom)) |> Shape
            | Shape ps -> 
                // Add the first point on to the end
                 let pss = Shapes3D.one_d (ps |> Array.append [|ps.[0]|]) (fun i ps -> ps |> Array.map (fun p -> (rotate(Quaternion(axis, -angle * (float i / float tes)))).Transform(p))) tes
                 let g = Shapes3D.createHardMesh pss (ps.Length - 1) tes
                 let top = convexFill(ps)
                 let bottom = convexFill(ps |> Array.map (fun p ->  ((rotate(Quaternion(axis,-angle))).Transform(p))))
                 top 
                 |> mergeGeom bottom
                 |> mergeGeom g
                 |> Geometry
            | Geometry g -> Geometry(g)
        

open Geometry3D
  
let window = Viewer(Topmost = true)
window.Show()

let add mv3d =  window.Viewport.Children.Add(mv3d); mv3d
let remove mv3d = window.Viewport.Children.Remove(mv3d) |> ignore

module Materials =
    let Goldenrod = DiffuseMaterial(Brushes.Goldenrod)

let point   = Point(Point3D(0.,0.,0.))
let line    = point.Extrude(Vector3D(1.,0.,0.))
let square  = line.Extrude(Vector3D(0.,1.,0.))
let cube    = square.Extrude(Vector3D(0.,0.,1.))

(* This functionality is not finished yet *)
//let circle  = line.Revolve(ZAxis,360.,10)
//let sphere  = circle.Revolve(YAxis,180.,5)
//let cubeAsPoints = cube.AsPoints()
//let sphereAsPoints = sphere.AsPoints()

open Shapes3D

let toModelVisual (material:Material) (mesh:MeshGeometry3D) = 
        ModelVisual3D(Content = GeometryModel3D(Geometry = mesh, BackMaterial = material, Material = material))

let goldVisual = toModelVisual Materials.Goldenrod


let visuals = 
    [|
        yield!
            [|point; line; square; cube|] 
            |> Array.mapi (fun i shape -> shape.Transform(translate(Vector3D(float i,0.,0.))).AsModelVisual(Materials.Goldenrod) |> add)
        yield!
            [|
                cylinder 5 30, (0.,2.)     
                cone 5 20, (2.,2.)
                circle 10 30, (4.,2.)
                helix 3. 3. 50, (6.,2.)
            |] 
            |> Array.map (
                fun (mesh,pos) -> 
                    let visual = goldVisual mesh
                    visual.Transform <- MatrixTransform3D(Matrix3D.translate(Vector3D(fst pos,snd pos,0.)))
                    visual |> add
                    )
    |]

//window.Reset()