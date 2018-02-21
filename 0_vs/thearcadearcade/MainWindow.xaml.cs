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


namespace thearcadearcade
{
    class Nestopia : GameHooks.Emulator
    {
        public Nestopia()
            : base("NES", "nestopia.exe")
        {
        }
    }

    class SampleWindowData : INotifyPropertyChanged
    {
        private int score;
        public int Score
        {
            get
            {
                return score;
            }
            set
            {
                score = value;
            }
        }

        private int lives;
        public int Lives
        {
            get
            {
                return lives;
            }
            set
            {
                lives = value;
            }
        }

        private int coins;
        public int Coins
        {
            get
            {
                return coins;
            }
            set
            {
                coins = value;
            }
        }

        private string time;
        public string Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
            }
        }

        public string state;
        public string State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window
    {
        Library.Player player;
        SampleWindowData windowData = new SampleWindowData();
        public MainWindow()
        {
            this.Hide();

            InitializeComponent();

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

            this.DataContext = windowData;

            Task task = Task.Run(async () => {
                do
                {
                    if (scene.CurrentActIndex == 0)
                    {
                        windowData.Coins = scene.CurrentAct.Game.GetMemoryArea("coins").GetByte(emulator);
                        windowData.OnPropertyChanged("Coins");
                        byte[] timeBytes =
                        {
                            scene.CurrentAct.Game.GetMemoryArea("timer1stDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer2ndDigit").GetByte(emulator),
                            scene.CurrentAct.Game.GetMemoryArea("timer3rdDigit").GetByte(emulator),
                        };
                        windowData.Time = string.Format("{0}{1}{2}", timeBytes[0], timeBytes[1], timeBytes[2]);
                        windowData.OnPropertyChanged("Time");
                    }

                    if (scene.CurrentActIndex == 1)
                    {
                        try
                        {
                            windowData.Score = scene.CurrentAct.Game.GetMemoryArea("kills").GetByte(emulator);
                            windowData.OnPropertyChanged("Score");
                            windowData.Lives = scene.CurrentAct.Game.GetMemoryArea("lives").GetByte(emulator);
                            windowData.OnPropertyChanged("Lives");
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    windowData.State = emulator.CurrentState.ToString();
                    windowData.OnPropertyChanged("State");

                    await Task.Delay(17);
                } while (true);
            });

            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
    }
    }
}
