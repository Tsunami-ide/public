module Tsunami.Public.Rhino3D.CirclesOnSphere
// based on script from RhinoPython Primer:
// http://python.rhino3d.com/content/130-RhinoPython-primer

// If you see a red squiggly below, you probably don't have Rhino 5 installed.
// Check the path exists.
// You can download the installer here: http://www.rhino3d.com/download
#r @"C:\Program Files\Rhinoceros 5.0 Evaluation (64-bit)\System\RhinoCommon.dll"

open System

let generate_circle_centers sphere_radius circle_radius = 
  let vertical_count = (Math.PI * sphere_radius) / (2.0 * circle_radius) |> Math.Floor
  seq { for phi in {-0.5 * Math.PI .. Math.PI / vertical_count .. 0.5 * Math.PI - 1e-8} do 
          let horizontal_count = 
            (2.0 * Math.PI * Math.Cos(phi) * sphere_radius) / (2.0 * circle_radius) 
            |> Math.Floor 
            |> fun c -> if c = 0.0 then 1.0 else c
          for theta in {0.0 .. (2.0 * Math.PI / horizontal_count) .. (2.0 * Math.PI - 1e-8)} do
            yield (sphere_radius * Math.Cos(theta) * Math.Cos(phi),
              sphere_radius * Math.Sin(theta) * Math.Cos(phi),
              sphere_radius * Math.Sin(phi))}

let sphere_radius, circle_radius = 10.0, 0.5
let circle_centers = generate_circle_centers sphere_radius circle_radius

let doc = Rhino.RhinoDoc.ActiveDoc
doc.Views.RedrawEnabled <- false
for circle_center in circle_centers do
  let x, y, z = circle_center
  let circle_normal = Rhino.Geometry.Point3d(x,y,z) - Rhino.Geometry.Point3d(0.0,0.0,0.0)
  let circle_plane = Rhino.Geometry.Plane(Rhino.Geometry.Point3d(x,y,z), circle_normal)
  doc.Objects.AddCircle(Rhino.Geometry.Circle(circle_plane, circle_radius)) |> ignore
  //doc.Objects.AddSurface(Rhino.Geometry.NurbsSurface.CreateFromTorus(Rhino.Geometry.Torus(circle_plane, 0.4, 0.1))) |> ignore
doc.Views.RedrawEnabled <- true
doc.Views.Redraw()
