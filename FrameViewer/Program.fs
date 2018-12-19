namespace FrameViewer
open System

module Pgm =
    open System.Windows.Forms

    [<EntryPoint>]
    [<STAThread>]
    let main argv = 
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault false
        
        use form = Viewer.createViewer()
 
        Application.Run(form);
        0 // return an integer exit code
