using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Interop;

using System.Windows;
using System.Windows.Data;

using CefSharp;

namespace thearcadearcade
{
    class Nestopia : GameHooks.Emulator
    {
        public Nestopia()
            : base("NES", "nestopia.exe")
        {
        }
    }

    class GameDataProvider : INotifyPropertyChanged
    {
        UI.CEF.ROMDataSupplier supplier;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GameDataProvider(ref UI.CEF.ROMData ROMdata)
        {
            supplier = new UI.CEF.ROMDataSupplier(ref ROMdata);
        }
    }

    public partial class MainWindow : Window
    {
        CefSharp.Wpf.ChromiumWebBrowser browser;
        Library.Player player;
        UI.CEF.ROMData data = new UI.CEF.ROMData();
        GameDataProvider dataProvider;

        public MainWindow()
        {
            InitializeComponent();

            dataProvider = new GameDataProvider(ref this.data);

            browser = this.FindName("UI") as CefSharp.Wpf.ChromiumWebBrowser;
            browser.MenuHandler = new UI.CEF.DebugToolsMenuHandler();

            Dictionary<string, Library.PlatformGameList> gamesPerPlatform = new Dictionary<string, Library.PlatformGameList>();

            string[] platformPaths = Directory.GetDirectories(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\"));
            foreach(string platformPath in platformPaths)
            {
                string platformName = Path.GetFileName(platformPath);
                gamesPerPlatform[platformName] = new Library.PlatformGameList(Path.Combine(platformPath, "games\\"), platformName);
            }


            Library.Scene scene = Library.Scene.FromJSON(Path.Combine(Directory.GetCurrentDirectory(), "scenes\\scene1\\config.json"), gamesPerPlatform);

            GameHooks.Emulator emulator = new Nestopia();
            player = new Library.Player(emulator, scene);

            this.DataContext = dataProvider;

            Task task = Task.Run(async () => {
                do
                {
                    if (scene.CurrentActIndex == 0)
                    {
                        data.Coins = scene.CurrentAct.Game.GetMemoryArea("coins").GetByte(emulator);
                        dataProvider.OnPropertyChanged("Coins");
                        byte[] timeBytes =
                        {
                            scene.CurrentAct.Game.GetMemoryArea("timer1stDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer2ndDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer3rdDigit").GetByte(emulator),
                        };
                        data.Time = string.Format("{0}{1}{2}", timeBytes[0], timeBytes[1], timeBytes[2]);
                        dataProvider.OnPropertyChanged("Time");
                    }

                    if (scene.CurrentActIndex == 1)
                    {
                        try
                        {
                            data.Score = scene.CurrentAct.Game.GetMemoryArea("kills").GetByte(emulator);
                            dataProvider.OnPropertyChanged("Score");
                            data.Lives = scene.CurrentAct.Game.GetMemoryArea("lives").GetByte(emulator);
                            dataProvider.OnPropertyChanged("Lives");
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    data.State = emulator.CurrentState.ToString();
                    dataProvider.OnPropertyChanged("State");

                    await Task.Delay(17);
                } while (true);
            });


        var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
    }
    }
}
