namespace FrameViewer

open OpenCvSharp
open System.Windows.Forms
open System.Drawing
open System.IO

module Viewer =
    open System.Windows.Forms

    let drawFrame (ctrl:Control, mat:Mat) =
        use g = ctrl.CreateGraphics()
        use bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat)
        let rect = RectangleF(0.f,0.f,float32 ctrl.Width,float32 ctrl.Height)
        g.DrawImage(bmp,0.f,0.f,rect,GraphicsUnit.Pixel)
        printfn "."
    
    let showOnImage image file = 
        let clipIn = new VideoCapture(file:string)
        for f in 1 .. clipIn.FrameCount do
            let m = new Mat()
            if clipIn.Read(m) then
                drawFrame(image, m)
            m.Release()
        clipIn.Release()

    let getFrame file n = 
        let clipIn = new VideoCapture(file:string)
        let _ = clipIn.Set(CaptureProperty.PosFrames, float n);
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

    let createViewer () =
        //state
        let file = ref None
        let form = new Form()
        let slider = new TrackBar()
        let grid = new TableLayoutPanel()
        let image = new System.Windows.Forms.PictureBox()
        let vtext = new Label()
        let imnCopy = new MenuItem("Copy")
        let imnSav = new MenuItem("Save frame as...")
        let imageMenu = new ContextMenu([|imnCopy; imnSav|])

        //form
        form.Width  <- 500
        form.Height <- 500
        form.Visible <- true 
        form.Text <- "Frame Viewer: Drop a video file here"
        form.AllowDrop <- true

        //grid
        grid.AutoSize <- true
        grid.ColumnCount <- 1
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90.f)) |> ignore
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100.f)) |> ignore
        grid.RowCount <- 2
        grid.RowStyles.Add(new RowStyle(SizeType.Percent,90.f)) |> ignore
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)) |> ignore
        grid.GrowStyle <-  TableLayoutPanelGrowStyle.AddColumns
        grid.Dock <- DockStyle.Fill

        //others
        vtext.Dock <- DockStyle.Right
        vtext.Margin <- Padding(10)
        slider.Dock <- DockStyle.Fill
        image.Dock <- DockStyle.Fill
        image.SizeMode <- PictureBoxSizeMode.Zoom
        image.ContextMenu <- imageMenu

        //add controls
        form.Controls.Add(grid)
        grid.Controls.Add(image,0,0)
        grid.SetColumnSpan(image,2)
        grid.Controls.Add(slider,0,1)
        grid.Controls.Add(vtext,1,1)

        //events
        let setFrame n = 
            !file |> Option.iter(fun f ->
            match getFrame f n with
            | Some m -> 
                image.Image <- m
                vtext.Text <- n.ToString()
                vtext.BackColor <- Color.Linen
            | None -> ())

        let saveFrame() = 
            let dlg = new SaveFileDialog()
            dlg.Filter <- "png (*.png) |*.png| bmp (*.bmp)|*.bmp|All (*.*)|*.*"
            dlg.RestoreDirectory <- true
            if (dlg.ShowDialog() = DialogResult.OK) then 
                try 
                    image.Image.Save(dlg.FileName)
                with ex -> 
                    MessageBox.Show(ex.Message) |> ignore
                    ()

        slider.ValueChanged.Add(fun e -> 
            vtext.BackColor <- Color.Gray
            vtext.Text <- slider.Value.ToString())

        slider.MouseUp.Add(fun e -> setFrame slider.Value)
        slider.KeyUp.Add(fun e-> setFrame slider.Value)
        
        form.DragEnter.Add(fun e->e.Effect <- DragDropEffects.All)

        form.DragDrop.Add(fun e -> 
            let files = e.Data.GetData(DataFormats.FileDrop) :?> string[]
            files |> Array.tryHead |> Option.iter (fun f-> 
            file := Some(f)
            slider.Maximum <- frameCount f - 1
            slider.Value <- 0
            setFrame 0
            let fn = Path.GetFileName(f)
            let dir = Path.GetDirectoryName(f)
            form.Text <- sprintf "%s [%s]" fn dir
            )
        )

        imnCopy.Click.Add(fun e-> !file |> Option.iter (fun f -> Clipboard.SetImage(image.Image)))
        imnSav.Click.Add(fun e-> saveFrame())

        form
