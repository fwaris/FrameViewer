namespace FrameViewerCore

open OpenCvSharp
open System.Drawing
open System.IO

module Api =    

    let getFrame file n = 
        let clipIn = new VideoCapture(file:string)
        let _ = clipIn.PosFrames <- n
        let mat = new Mat()
        let resp = 
            if clipIn.Read(mat) then
                    let ptr = mat.CvPtr
                    let step = mat.Step()
                    //let bmp = new Bitmap(mat.Cols, mat.Rows, step |> int,PixelFormat.Format24bppRgb,ptr);
                    let bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat)
                
                    Some(bmp)
                else
                    None
        mat.Release()
        clipIn.Release() 
        resp

    let frameCount f = 
        let clipIn = new VideoCapture(f:string)
        let fc = clipIn.FrameCount
        clipIn.Release()
        fc

