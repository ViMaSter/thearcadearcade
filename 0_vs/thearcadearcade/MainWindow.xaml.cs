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
            : base("C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/platforms/nes/executable/nestopia.exe")
        {
        }
    }

    class SMB : GameHooks.Game, INotifyPropertyChanged
    {
        public SMB()
            : base("C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/scenes/game1/rom.nes", "NTSC", "C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/platforms/nes/executable/nestopia.exe")
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int currentCoins = 0;
        public int CurrentCoins
        {
            get
            {
                return currentCoins;
            }
            set
            {
                if (currentCoins != value)
                {
                    currentCoins = value;
                    OnPropertyChanged();
                }
            }
        }

        private string currentTime = "";
        public string CurrentTime
        {
            get
            {
                return currentTime;
            }
            set
            {
                if (currentTime != value)
                {
                    currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private GameHooks.Emulator.State currentState = GameHooks.Emulator.State.ERROR;
        public GameHooks.Emulator.State CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                if (currentState != value)
                {
                    currentState = value;
                    OnPropertyChanged();
                    OnPropertyChanged("CurrentStateText");
                }
            }
        }
        public string CurrentStateText
        {
            get
            {
                return currentState.ToString();
            }
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Nestopia emu = new Nestopia();
            SMB smb = new SMB();

            this.DataContext = smb;

            Task task = Task.Run(async () => {
                do
                {
                    smb.CurrentState = emu.CurrentState;

                    if (emu.CurrentState == GameHooks.Emulator.State.READY)
                    {
                        byte[] isReadyFlag = new byte[1];
                        emu.ReadGameMemory(0, 1, out isReadyFlag);

                        if (isReadyFlag[0] == 0xFF)
                        {
                            int gameStartStatus = emu.StartGame(smb);
                            if (gameStartStatus == 0)
                            {
                                emu.CurrentState = GameHooks.Emulator.State.RUNNING;
                            }
                            else
                            {
                                Console.WriteLine("Couldn't launch game {0}: Error code {1}", smb.Name, gameStartStatus);
                                emu.CurrentState = GameHooks.Emulator.State.ERROR;
                            }
                        }
                    }

                    if (emu.CurrentState != GameHooks.Emulator.State.RUNNING)
                    {
                        continue;
                    }

                    GameHooks.MemoryArea coins = new GameHooks.MemoryArea(0x746, 1);
                    smb.CurrentCoins = coins.GetByte(emu);
                    GameHooks.MemoryArea time1 = new GameHooks.MemoryArea(0x7E0, 1);
                    GameHooks.MemoryArea time2 = new GameHooks.MemoryArea(0x7E1, 1);
                    GameHooks.MemoryArea time3 = new GameHooks.MemoryArea(0x7E2, 1);
                    byte[] timeBytes =
                    {
                        time1.GetByte(emu),
                        time2.GetByte(emu),
                        time3.GetByte(emu)
                    };
                    smb.CurrentTime = string.Format("{0}{1}{2}", timeBytes[0], timeBytes[1], timeBytes[2]);

                    await Task.Delay(17);
                } while (true);
            });
        }
    }
}
