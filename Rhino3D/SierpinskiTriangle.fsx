#r @"C:\Program Files\Rhinoceros 5.0 Evaluation (64-bit)\System\RhinoCommon.dll"
open Rhino
open Rhino.Geometry
open Rhino.Commands
open System.Drawing
open Microsoft.FSharp.Math

[<AutoOpen>]
module GE =
    let redraw() = Rhino.RhinoDoc.ActiveDoc.Views.Redraw()

    let addSphere(x:float, y:float, z:float, radius : float) =
        let doc = Rhino.RhinoDoc.ActiveDoc
        let mutable sp = new Sphere(Plane.WorldXY, radius)
        sp.Center <- Point3d(x,y,z)
        if (not (doc.Objects.AddSphere(sp) = System.Guid.Empty)) then Result.Success else Result.Failure

    let addMesh(vertices : (float*float*float)[], faces : (int*int*int)[]) =
        let doc = Rhino.RhinoDoc.ActiveDoc
        let mesh = new Mesh()
        for (x,y,z) in vertices do mesh.Vertices.Add(x,y,z) |> ignore
        for (a,b,c) in faces do mesh.Faces.AddFace(a,b,c) |> ignore
        mesh.Normals.ComputeNormals() |> ignore
        mesh.Compact() |> ignore
        if (not (doc.Objects.AddMesh(mesh) = System.Guid.Empty)) then Result.Success else Result.Failure

    let cube =
        let verticies = 
            [|
              (-0.5, 1.0, 0.5); ( 0.5, 1.0, 0.5);
              (-0.5, 0.0, 0.5); ( 0.5, 0.0, 0.5);
              ( 0.5, 1.0,-0.5); (-0.5, 1.0,-0.5);
  
              ( 0.5, 0.0,-0.5); (-0.5, 0.0,-0.5);
              (-0.5, 1.0,-0.5); (-0.5, 1.0, 0.5);
              (-0.5, 0.0,-0.5); (-0.5, 0.0, 0.5);

              ( 0.5, 1.0, 0.5); ( 0.5, 1.0,-0.5);
              ( 0.5, 0.0, 0.5); ( 0.5, 0.0,-0.5);

              (-0.5, 1.0,-0.5); ( 0.5, 1.0,-0.5);
              (-0.5, 1.0, 0.5); ( 0.5, 1.0, 0.5);

              ( 0.5, 0.0,-0.5); (-0.5, 0.0,-0.5);
              ( 0.5, 0.0, 0.5); (-0.5, 0.0, 0.5); 
            |]

        let faces = [|(0,2,1);(1,2,3);(4,6,5);(5,6,7);(8,10,9);(9,10,11);(12,14,13);(13,14,15);(16,18,17);(17,18,19);(20,22,21);(21,22,23);|]
        (verticies,faces)

    let addTriangles (xs:(Point3d*Point3d*Point3d)[]) =
        let doc = Rhino.RhinoDoc.ActiveDoc
        let mesh = new Mesh()
        xs |> Array.collect (fun (a,b,c) -> [|a;b;c|]) |> Array.iter (fun x -> mesh.Vertices.Add(x) |> ignore)
        xs 
            |> Array.mapi (fun i _ -> (3*i,3*i+1,3*i+2)) 
            |> Array.iter (fun (a,b,c) -> mesh.Faces.AddFace(a,b,c) |> ignore)
        mesh.Normals.ComputeNormals() |> ignore
        mesh.Compact() |> ignore
        if (not (doc.Objects.AddMesh(mesh) = System.Guid.Empty)) then Result.Success else Result.Failure
    
    
    let midPoint (p1:Point3d,p2:Point3d) = p1 - ((p1-p2) / 2.)

    let sierpinski (depth:int) =
        let rec sierpinskir (p1:Point3d,p2:Point3d,p3:Point3d) (depth:int) =
            match depth with
            | 0 -> [|(p1,p2,p3)|]
            | _ ->
                let p1p2 = midPoint(p1,p2)
                let p2p3 = midPoint(p2,p3)
                let p3p1 = midPoint(p3,p1)
                [|
                    yield! sierpinskir (p1,p1p2,p3p1) (depth - 1)
                    yield! sierpinskir (p1p2,p2,p2p3) (depth - 1)
                    yield! sierpinskir (p2p3,p3,p3p1) (depth - 1)
                |]
        sierpinskir (Point3d(0.,0.,0.),Point3d(1.,0.,0.),Point3d(0.5,sqrt(0.5),0.)) depth

    let tetrix (depth:int) =
        let rec tetrixr (p1:Point3d,p2:Point3d,p3:Point3d,p4:Point3d) (depth:int) =  
            match depth with
            | 0 -> [|(p1,p2,p3,p4)|]
            | _ ->
                let p1p2 = midPoint(p1,p2)
                let p1p3 = midPoint(p1,p3)
                let p1p4 = midPoint(p1,p4)

                let p2p3 = midPoint(p2,p3)
                let p2p4 = midPoint(p2,p4)
        
                let p3p4 = midPoint(p3,p4)
                [|
                    yield! tetrixr (p1,p1p2,p1p3,p1p4) (depth - 1)
                    yield! tetrixr (p1p2,p2,p2p3,p2p4) (depth - 1)
                    yield! tetrixr (p1p3,p2p3,p3,p3p4) (depth - 1)
                    yield! tetrixr (p1p4,p2p4,p3p4,p4) (depth - 1)
                |]    

        tetrixr (Point3d(-1.,-1./sqrt(3.),-1./sqrt(6.)),Point3d(1.,-1./sqrt(3.),-1./sqrt(6.)),Point3d(0.,2./sqrt(3.),-1./sqrt(6.)),Point3d(0.,0.,3./sqrt(6.))) depth
        |> Array.collect (fun (p1,p2,p3,p4) -> [|(p1,p2,p3);(p1,p2,p4);(p1,p3,p4);(p2,p4,p3)|])

        
for i in 0..3 do 
    for j in 0..3 do 
        for k in 0..3 do 
            GE.addSphere(float i, float j, float k, 0.1) |> ignore

GE.sierpinski 8
|> GE.addTriangles


GE.tetrix  6
|> GE.addTriangles

GE.tetrix 6 |> GE.addTriangles
