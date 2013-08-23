#r "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.dll"
#r "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.Extension.dll"
#r "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.Extension.Graphics.Direct3D9.dll"
#r "System.Drawing"
#r "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.dll"
#r "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.Direct3D9.dll"
#r "cache:http://tsunami.io/assemblies/GPGPU/SharpDX.RawInput.dll"

open System
open Alea.CUDA
open Alea.CUDA.Extension
open Alea.CUDA.Extension.Graphics.Direct3D9

[<Struct;Align(16)>]
type Param =
    val mutable rows : int
    val mutable cols : int
    val mutable time : float

let fillIRM =
    let transform =
        <@ fun (r:int) (c:int) (p:Param) ->
            let u = float(c) / float(p.cols)
            let v = float(r) / float(p.rows)
            let u = u * 2.0 - 1.0
            let v = v * 2.0 - 1.0
            let freq = 4.0
            sin(u * freq + p.time) * cos(v * freq + p.time) @>

    fun () ->
        printf "Compiling wave surface kernel..."
        let irm = PMatrix.fillip transform |> genirm
        printfn "[OK]"
        irm
    |> Lazy.Create

let plottingLoop (order:Util.MatrixStorageOrder) (rows:int) (cols:int) renderType (context:Context) =
    pcalc {
        let fill = context.Worker.LoadPModule(fillIRM.Value).Invoke
        let! surface = DMatrix.createInBlob context.Worker order rows cols
        let param = Param(rows = rows, cols = cols, time = 0.0)
        do! fill param surface
        let extend minv maxv = let maxv', _ = SurfacePlotter.defaultExtend minv maxv in maxv', 0.5
        do! SurfacePlotter.plottingLoop context surface extend renderType }
    |> PCalc.run

let animationLoop (order:Util.MatrixStorageOrder) (rows:int) (cols:int) renderType (context:Context) =
    pcalc {
        let fill = context.Worker.LoadPModule(fillIRM.Value).Invoke
        let! surface = DMatrix.createInBlob context.Worker order rows cols
        let extend minv maxv = let maxv', _ = SurfacePlotter.defaultExtend minv maxv in maxv', 0.5
        let gen (time:float) = pcalc {
            let param = Param(rows = rows, cols = cols, time = time / 400.0)
            do! fill param surface
            return surface, extend }
        do! SurfacePlotter.animationLoop context order rows cols gen renderType None }
    |> PCalc.run


let device = Device.AllDevices.[0]
let param = { (Param.Create(device)) with FormTitle = "Wave Surface Plotting"
                                          DrawingSize = Drawing.Size(1024, 768) }
let loop = plottingLoop Util.ColMajorOrder 3000 3000 SurfacePlotter.Mesh
let app = Application(param, loop)

app.Start(true,false)