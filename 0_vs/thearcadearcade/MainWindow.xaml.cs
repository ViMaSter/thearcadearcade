using System.IO;
using System.Windows;

namespace thearcadearcade
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
#if DEBUG
            CefSharp.Cef.Initialize(new CefSharp.CefSettings
            {
                RemoteDebuggingPort = 8999
            });
#endif

            InitializeComponent();

#if DEBUG
            UI.Address = Path.Combine(Directory.GetCurrentDirectory(), "ui\\ingame.html#debug");
#else
            UI.Address = Path.Combine(Directory.GetCurrentDirectory(), "ui\\ingame.html");
#endif

            UI.MenuHandler = new UI.CEF.DebugToolsMenuHandler();

            UI.JavascriptObjectRepository.ResolveObject += (sender, currentEvent) =>
            {
                CefSharp.IJavascriptObjectRepository repository = currentEvent.ObjectRepository;
                if (currentEvent.ObjectName == "app")
                {
                    AppCallbacks callbacks = new AppCallbacks(UI);
                    ((TheArcadeArcadeApp)Application.Current).SetAppCallbacks(callbacks);
                    repository.Register("app", callbacks, true);
                }
            };

            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
            UI.Width = SystemParameters.WorkArea.Width;
            UI.Height = SystemParameters.WorkArea.Height;
        }
    }
}
