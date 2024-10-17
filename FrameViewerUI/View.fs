namespace FrameViewerUI
open System
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Threading
open Avalonia.Platform.Storage
open FrameViewerCore
open Avalonia.Media
open Avalonia.Controls.Primitives
open System.Threading
open Avalonia.Media.Imaging
open Avalonia.Controls.Shapes

module ImageUtils = 
    let convertToAvaloniaBitmap (bitmap: System.Drawing.Bitmap) =
        let bitmapdata = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        let bitmap1 = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul, bitmapdata.Scan0, new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height), new Avalonia.Vector(96, 96), bitmapdata.Stride)
        bitmap.UnlockBits(bitmapdata)
        bitmap1

[<AbstractClass; Sealed>]
type Views =

    static member main (window:Window)  =
        Component (fun ctx ->
            let frame = ctx.useState (0)
            let frameInc = ctx.useState (1)
            let frameCount = ctx.useState (0)
            let filePath : IWritable<string> = ctx.useState null
            let image :IWritable<Bitmap> = ctx.useState (null)
            let busy = ctx.useState false

            let setBusy v = Dispatcher.UIThread.InvokeAsync(fun () -> busy.Set v) |> ignore

            let initFile file =
                async {
                    try                       
                        let frames = Api.frameCount file
                        let bmp = Api.getFrame file 0
                        match bmp with
                        | Some bmp -> 
                            frame.Set 0
                            frameCount.Set frames                        
                            image.Set (ImageUtils.convertToAvaloniaBitmap bmp)
                        | None -> printfn "Error: Could not get first frame"
                    with ex ->
                        printfn "Error: %s" (if ex.InnerException <> null then ex.InnerException.Message else ex.Message)
                }
                |> Async.Start                

            let setFrame file n=   
                async {
                    try 
                        setBusy true
                        let bmp = Api.getFrame file n
                        match bmp with
                        | Some bmp -> 
                            image.Set (ImageUtils.convertToAvaloniaBitmap bmp)
                        | None -> printfn "Error: Could not get frame %d" n
                        setBusy false
                    with ex ->
                        setBusy false
                        printfn "Error: %s" (if ex.InnerException <> null then ex.InnerException.Message else ex.Message)
                }
                |> Async.Start

            let haveFile() = String.IsNullOrWhiteSpace filePath.Current |> not                

            ctx.useEffect ((fun () -> if haveFile() then initFile filePath.Current), [EffectTrigger.AfterChange filePath])
            ctx.useEffect ((fun () -> if haveFile() then setFrame filePath.Current frame.Current),[EffectTrigger.AfterChange frame])

            let slider = ref None

            //root view
            DockPanel.create [
                DockPanel.children [
                    Grid.create [
                        Grid.rowDefinitions "50,1*,50"
                        Grid.children [
                            StackPanel.create [
                                Grid.row 0
                                StackPanel.orientation Orientation.Horizontal                                
                                StackPanel.children [                                 
                                    Ellipse.create [
                                        TextBlock.margin (Thickness(2.0))
                                        Shape.width 10; 
                                        Shape.height 10; 
                                        Shape.fill (if busy.Current then Brushes.Red else Brushes.Green)
                                        Shape.verticalAlignment VerticalAlignment.Center
                                    ]
                                    Button.create [
                                        TextBlock.margin (Thickness(2.0))
                                        Button.content (TextBlock.create [TextBlock.text "Open"])
                                        Button.onClick (fun _ -> 
                                            async {
                                                try 
                                                    let filter = FilePickerFileType("Video Files", Patterns =  ["*.mp4";"*.avi";"*.mov";"*.wmv"])
                                                    let! file = Dialogs.showFileDialog window.StorageProvider "Open Video" [filter]
                                                    filePath.Set (string file.[0].Path)
                                                with ex ->
                                                    printfn "Error: %s" (if ex.InnerException <> null then ex.InnerException.Message else ex.Message)
                                            }
                                            |> Async.Start
                                        )
                                    ]
                                    TextBlock.create [
                                        TextBlock.margin (Thickness(2.0))
                                        Grid.row 0
                                        Grid.column 1
                                        TextBlock.horizontalAlignment HorizontalAlignment.Left
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text filePath.Current
                                    ]
                                    TextBlock.create [
                                        TextBlock.margin (Thickness(2.0))
                                        Grid.row 0
                                        Grid.column 1
                                        TextBlock.horizontalAlignment HorizontalAlignment.Left
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text $"Frame Count: {frameCount.Current}"
                                    ]
                                ]
                            ]
                            Image.create [      
                                Grid.row 1
                                Image.source image.Current
                            ]
                            Grid.create [
                                Grid.row 2
                                Grid.columnDefinitions "1*,50"
                                Grid.children [
                                    Slider.create [
                                        Slider.init (fun t -> slider.Value <- Some t)
                                        Slider.minimum 0.
                                        Slider.maximum (float frameCount.Current)
                                        Slider.value frameInc.Current
                                        Slider.onPointerReleased (fun v -> slider.Value |> Option.iter (fun s -> frame.Set (int s.Value ))) 
                                        Slider.onValueChanged  (fun v -> frameInc.Set (int v))
                                    ]
                                    TextBlock.create [
                                        TextBlock.margin (Thickness(0.5))
                                        Grid.row 0
                                        Grid.column 1
                                        TextBlock.horizontalAlignment HorizontalAlignment.Left
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text (string frameInc.Current)
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        )
