using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;


namespace thearcadearcade
{
    class Nestopia : GameHooks.Emulator
    {
        public Nestopia()
            : base("NES", "C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/platforms/nes/executable/nestopia.exe")
        {
        }
    }

    class SampleWindowData : INotifyPropertyChanged
    {
        private GameHooks.Game gameMemory;
        public GameHooks.Game GameMemory
        {
            get
            {
                return gameMemory;
            }
            set
            {
                gameMemory = value;
                PropertyChangedEventHandler eh = new PropertyChangedEventHandler(ChildChanged);
                gameMemory.PropertyChanged += eh;
                OnPropertyChanged("gameMemory");
            }
        }
        private void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("gameMemory");
        }

        private GameHooks.Emulator emulator;
        public GameHooks.Emulator Emulator
        {
            get
            {
                return emulator;
            }
            set
            {
                emulator = value;
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
                OnPropertyChanged("Coins");
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
                OnPropertyChanged("Time");
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
        Player player;
        SampleWindowData windowData = new SampleWindowData();
        public MainWindow()
        {
            InitializeComponent();

            Dictionary<string, Library.PlatformGameList> gamesPerPlatform = new Dictionary<string, Library.PlatformGameList>();

            string[] platformPaths = Directory.GetDirectories(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\"));
            foreach(string platformPath in platformPaths)
            {
                string platformName = Path.GetFileName(platformPath);
                gamesPerPlatform[platformName] = new Library.PlatformGameList(Path.Combine(platformPath, "games\\"), platformName);
            }


            Library.Scene scene = Library.Scene.FromJSON(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "scenes\\scene1\\config.json")), gamesPerPlatform);
            windowData.Emulator = new Nestopia();
            windowData.GameMemory = scene.CurrentAct.Game;

            player = new Player(windowData.Emulator, windowData.GameMemory);

            this.DataContext = windowData;

            Task task = Task.Run(async () => {
                do
                {

                    windowData.Coins = windowData.GameMemory.GetMemoryArea("coins").GetByte(windowData.Emulator);
                    byte[] timeBytes =
                    {
                        windowData.GameMemory.GetMemoryArea("timer1stDigit").GetByte(windowData.Emulator),
                        windowData.GameMemory.GetMemoryArea("timer2ndDigit").GetByte(windowData.Emulator),
                        windowData.GameMemory.GetMemoryArea("timer3rdDigit").GetByte(windowData.Emulator)
                    };
                    windowData.Time = string.Format("{0}{1}{2}", timeBytes[0], timeBytes[1], timeBytes[2]);
                    await Task.Delay(17);
                } while (true);
            });
        }
    }
}
