using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace AppCallbacks
{
}

namespace thearcadearcade
{
    public class AppCallbacks
    {
        CefSharp.Wpf.ChromiumWebBrowser browser;

        public AppCallbacks(CefSharp.Wpf.ChromiumWebBrowser browser)
        {
            this.browser = browser;
        }

        public void Exit()
        {
            Environment.Exit(0);
        }
    }

    class Nestopia : GameHooks.Emulator
    {
        public Nestopia()
            : base("NES", "nestopia.exe")
        {
        }
    }

    public partial class TheArcadeArcadeApp : Application
    {
        Library.Player player;
        UI.CEF.ROMData data = new UI.CEF.ROMData();
        UI.CEF.ROMDataSupplier supplier;
        AppCallbacks uiCallbacks;

        public AppCallbacks SetAppCallbacks(AppCallbacks callbacks)
        {
            uiCallbacks = callbacks;
            return uiCallbacks;
        }

        public TheArcadeArcadeApp()
        {
            supplier = new UI.CEF.ROMDataSupplier(ref data);

            States.Manager manager = new States.Manager();
            manager.SetState(new States.Initialization());

            Dictionary<string, Library.PlatformGameList> gamesPerPlatform = new Dictionary<string, Library.PlatformGameList>();

            string[] platformPaths = Directory.GetDirectories(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\"));
            foreach (string platformPath in platformPaths)
            {
                string platformName = Path.GetFileName(platformPath);
                gamesPerPlatform[platformName] = new Library.PlatformGameList(Path.Combine(platformPath, "games\\"), platformName);
            }

            Library.Scene scene = Library.Scene.FromJSON(Path.Combine(Directory.GetCurrentDirectory(), "scenes\\scene1\\config.json"), gamesPerPlatform);

            GameHooks.Emulator emulator = new Nestopia();
            player = new Library.Player(emulator, scene);

            Task task = Task.Run(async () => {
                do
                {
                    if (scene.CurrentActIndex == 0)
                    {
                        data.Coins = scene.CurrentAct.Game.GetMemoryArea("coins").GetByte(emulator);
                        byte[] timeBytes =
                        {
                            scene.CurrentAct.Game.GetMemoryArea("timer1stDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer2ndDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer3rdDigit").GetByte(emulator),
                        };
                        data.Time = string.Format("{0}{1}{2}", timeBytes[0], timeBytes[1], timeBytes[2]);
                    }

                    if (scene.CurrentActIndex == 1)
                    {
                        data.Score = scene.CurrentAct.Game.GetMemoryArea("kills").GetByte(emulator);
                        data.Lives = scene.CurrentAct.Game.GetMemoryArea("lives").GetByte(emulator);
                    }

                    data.State = emulator.CurrentState.ToString();

                    await Task.Delay(17);
                } while (true);
            });
        }
    }
}
