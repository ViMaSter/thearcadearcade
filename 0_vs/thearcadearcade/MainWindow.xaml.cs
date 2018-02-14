using System;
using System.Collections;
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
        SampleWindowData windowData = new SampleWindowData();
        public MainWindow()
        {
            InitializeComponent();

            windowData.Emulator = new Nestopia();
            windowData.GameMemory = GameHooks.Game.FromJSON(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\nes\\games\\super_mario_bros.json")));

            this.DataContext = windowData;

            Task task = Task.Run(async () => {
                do
                {
                    windowData.Emulator.CurrentState = windowData.Emulator.CurrentState;

                    if (windowData.Emulator.CurrentState == GameHooks.Emulator.State.READY)
                    {
                        byte[] isReadyFlag = new byte[1];
                        windowData.Emulator.ReadGameMemory(0, 1, out isReadyFlag);

                        if (isReadyFlag[0] == 0xFF)
                        {
                            int gameStartStatus = windowData.Emulator.StartGame(windowData.GameMemory);
                            if (gameStartStatus == 0)
                            {
                                windowData.Emulator.CurrentState = GameHooks.Emulator.State.RUNNING;
                            }
                            else
                            {
                                Console.WriteLine("Couldn't launch game {0}: Error code {1}", windowData.GameMemory.Name, gameStartStatus);
                                windowData.Emulator.CurrentState = GameHooks.Emulator.State.ERROR;
                            }
                        }
                    }

                    if (windowData.Emulator.CurrentState != GameHooks.Emulator.State.RUNNING)
                    {
                        continue;
                    }

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
