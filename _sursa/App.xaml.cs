using System.Windows;

namespace FallenStickers
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // test intern: FallenStickers.exe --prep in.jpg out.png
            // FallenStickers.exe --prep in.jpg out.png ["text"]
            if (e.Args.Length >= 3 && e.Args[0] == "--prep")
            {
                int code = 0;
                try
                {
                    var bg = ImagePrep.LoadBase(e.Args[1]);
                    var st = new TextStyle { Text = e.Args.Length > 3 ? e.Args[3] : "" };
                    var img = ImagePrep.Compose(bg, st);
                    int w = img.PixelWidth, h = img.PixelHeight;
                    var png = ImagePrep.Encode(img);
                    System.IO.File.WriteAllBytes(e.Args[2], png);
                    System.IO.File.WriteAllText(e.Args[2] + ".txt", $"{w}x{h} {png.Length}");
                }
                catch (System.Exception ex) { System.IO.File.WriteAllText(e.Args[2] + ".txt", "ERR " + ex.Message); code = 1; }
                Shutdown(code);
                return;
            }
            new MainWindow(e.Args).Show();
        }
    }
}
