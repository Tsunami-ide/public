#r "cache:http://tsunami.io/assemblies/GPGPU/Alea.CUDA.dll"

open Alea.CUDA

let pfunct = cuda {
    let! kernel =
        <@ fun (A:DevicePtr<float>) (B:DevicePtr<float>) (C:DevicePtr<float>) ->
            let tid = threadIdx.x
            C.[tid] <- A.[tid] + B.[tid] @>
        |> defineKernelFunc

    return PFunc(fun (m:Module) (A:float[]) (B:float[]) ->
        use A = m.Worker.Malloc(A)
        use B = m.Worker.Malloc(B)
        use C = m.Worker.Malloc(A.Length)
        let lp = LaunchParam(1, A.Length)
        kernel.Launch m lp A.Ptr B.Ptr C.Ptr
        C.ToHost()) }
        
let worker = Engine.workers.DefaultWorker

let pfuncm = worker.LoadPModule(pfunct)

let a = [| 1.1; 2.2; 3.3 |]
let b = [| 0.5; 0.5; 0.5 |]

let c = pfuncm.Invoke a b

printfn "%A" c
