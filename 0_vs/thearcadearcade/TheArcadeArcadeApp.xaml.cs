using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
            Application.Current.Dispatcher.Invoke(() =>
                System.Windows.Application.Current.Shutdown(0)
            );
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

            States.Manager stateManager = new States.Manager();
            stateManager.SetState(new States.Initialization());

            Library.Manager libraryManager = new Library.Manager();

            Library.Scene scene = Library.Scene.FromJSON(Path.Combine(Directory.GetCurrentDirectory(), "scenes\\scene1\\config.json"), libraryManager.GamesPerPlatform);

            GameHooks.Emulator emulator = libraryManager.GetEmulatorByPlatform("NES");
            player = new Library.Player(emulator, scene);

            Task task = Task.Run(async () => {
                do
                {
                    if (player.CurrentState == Library.Player.State.RUNNING)
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
                    }
                    if (player.CurrentState == Library.Player.State.ERROR)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            System.Windows.Application.Current.Shutdown(1)
                        );
                    }
                } while (true);
            });
        }
    }
}
