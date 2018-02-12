﻿using System;
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

    static class WinAPI
    {
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int MEM_COMMIT = 0x00001000;
        public const int PAGE_READWRITE = 0x04;
        public const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        // REQUIRED STRUCTS
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public uint RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public UIntPtr minimumApplicationAddress;
            public UIntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
    }
    
    partial class Emulator
    {
        public enum State
        {
            INITIALIZING,
            READY,
            RUNNING,
            ERROR
        };
        State currentState = State.INITIALIZING;
        public State CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                currentState = value;
            }
        }

        string platformName;
        public string PlatformName
        {
            get { return platformName; }
        }

        string loadedGame;
        public string LoadedGame
        {
            get { return loadedGame; }
        }
        Process CurrentProcess;
        IntPtr ProcessHandle;
        UIntPtr[] ProcessAddressSpan = new UIntPtr[2];

        int BaseGameMemoryAddress;

        /// <summary>
        /// Sets up Process- and Process-handle-container
        /// </summary>
        void SetupProcessHandle()
        {
            // getting minimum & maximum address
            WinAPI.SYSTEM_INFO sys_info = new WinAPI.SYSTEM_INFO();
            WinAPI.GetSystemInfo(out sys_info);

            ProcessAddressSpan[0] = sys_info.minimumApplicationAddress;
            ProcessAddressSpan[1] = sys_info.maximumApplicationAddress;

            ProcessHandle = WinAPI.OpenProcess(WinAPI.PROCESS_QUERY_INFORMATION | WinAPI.PROCESS_WM_READ, false, CurrentProcess.Id);
        }

        void GetLookupValue(out byte[] expectedValue)
        {
            expectedValue = new byte[0x3CF];
            for (int i = 0; i < 0x3CF; i++)
            {
                expectedValue[i] = 0xff;
            }
        }

        int GetBaseMemoryOffset()
        {
            return 0xC8;
        }

        /// <summary>
        /// Sets `result` to be the memory address at which game memory start
        /// </summary>
        /// <returns>
        /// Return codes:
        /// 0 = success
        /// 1 = couldn't find the base address (result will be -1)
        /// 2 = error querying process info
        /// </returns>
        int SetBaseMemory()
        {
            // this will store any information we get from VirtualQueryEx()
            WinAPI.MEMORY_BASIC_INFORMATION mem_basic_info = new WinAPI.MEMORY_BASIC_INFORMATION();

            int bytesRead = 0;  // number of bytes read with ReadProcessMemory
            for (long currentProcessAddress = (long)ProcessAddressSpan[0]; currentProcessAddress < (long)ProcessAddressSpan[1]; currentProcessAddress += mem_basic_info.RegionSize)
            {
                IntPtr currentProcessAddressPtr = new IntPtr(currentProcessAddress);
                // 28 = sizeof(MEMORY_BASIC_INFORMATION)
                WinAPI.VirtualQueryEx(ProcessHandle, currentProcessAddressPtr, out mem_basic_info, 28);

                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != 0)
                {
                    Console.WriteLine("Error querying process info: Error code {0}", win32Error);
                    CurrentState = State.ERROR;
                    return 2;
                }

                // if this memory chunk is accessible
                if (mem_basic_info.Protect == WinAPI.PAGE_READWRITE && mem_basic_info.State == WinAPI.MEM_COMMIT)
                {
                    byte[] expectedBuffer = new byte[0];
                    GetLookupValue(out expectedBuffer);

                    byte[] buffer = new byte[expectedBuffer.Length];

                    // read everything in the buffer above
                    WinAPI.ReadProcessMemory((int)ProcessHandle, mem_basic_info.BaseAddress + GetBaseMemoryOffset(), buffer, buffer.Length, ref bytesRead);

                    if (StructuralComparisons.StructuralEqualityComparer.Equals(buffer, expectedBuffer))
                    {
                        BaseGameMemoryAddress = mem_basic_info.BaseAddress + GetBaseMemoryOffset();
                        return 0;
                    }
                }
            }

            return 1;
        }

        /// <summary>
        /// Attempt to read from the process' memory
        /// </summary>
        /// <param name="address">Address relative to the game's memory</param>
        /// <param name="size">Amount of bytes that should be read</param>
        /// <param name="buffer">Buffer containing the bytes on success</param>
        /// <returns>
        /// Return code:
        /// 0 = success
        /// 1 = Couldn't read from memory
        /// </returns>
        public int ReadGameMemory(int address, int size, out byte[] buffer)
        {
            int bytesRead = 0;
            buffer = new byte[size];
            WinAPI.ReadProcessMemory((int)ProcessHandle, BaseGameMemoryAddress + address, buffer, size, ref bytesRead);
            return bytesRead == 0 ? 1 : 0;
        }

        public Emulator(string _platformName)
        {
            platformName = _platformName;
            if (!StartProcess(""))
            {
                CurrentState = State.ERROR;
                Console.WriteLine(string.Format("Error starting emulator process {0}", platformName));
                return;
            }
            CurrentState = State.INITIALIZING;
            TryToAttachToProcess();
        }

        ~Emulator()
        {
            CurrentProcess.Kill();
        }

        bool StartProcess(string commandLineArguments)
        {
            CurrentProcess = new Process();
            CurrentProcess.StartInfo.FileName = platformName;
            CurrentProcess.StartInfo.Arguments= commandLineArguments;
            // resolve emulator name from platform
            try
            {
                return CurrentProcess.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error starting emulator process {0}: {1}", platformName, e.ToString()));
                return false;
            }
        }

        void TryToAttachToProcess()
        {
            var task = Task.Run(async () =>
            {
                do
                {
                    SetupProcessHandle();
                    int baseMemorySetupStatusCode = SetBaseMemory();
                    if (baseMemorySetupStatusCode != 0)
                    {
                        Console.WriteLine("Couldn't find base memory; is there already a game running?");
                        await Task.Delay(17);
                        continue;
                    }

                    CurrentState = State.READY;
                    await Task.Delay(17);
                    break;
                }
                while (true);
            });
        }

        /// <summary>
        /// Start a game using this emulator
        /// </summary>
        /// <param name="game">Game to start</param>
        /// <returns>
        /// Return code:
        /// 0 = success
        /// 1 = Emulator platform and game platform do not match!
        /// </returns>
        public void StartGame(Game game)
        {
            if (game.PlatformName != this.PlatformName)
            {
                return;
            }

            loadedGame = game.GameName;
            StartProcess(game.GameName);
        }
    }

    class Nestopia : Emulator
    {
        public Nestopia()
            : base("C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/platforms/nes/executable/nestopia.exe")
        {
        }
    }

    partial class Game
    {
        public string GameName
        {
            get { return gameName; }
        }
        string gameName;
        public string PlatformName
        {
            get { return platformName; }
        }
        string platformName;
        public Game(string _gameName, string _platformName)
        {
            gameName = _gameName;
            platformName = _platformName;
        }
    }

    class SMB : Game, INotifyPropertyChanged
    {
        public SMB()
            : base("C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/scenes/game1/rom.nes", "C:/Users/vmahnke/Desktop/em/0_git/1_dependencies/platforms/nes/executable/nestopia.exe")
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

        private Emulator.State currentState = Emulator.State.ERROR;
        public Emulator.State CurrentState
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

                    if (emu.CurrentState == Emulator.State.READY)
                    {
                        byte[] isReadyFlag = new byte[1];
                        emu.ReadGameMemory(0, 1, out isReadyFlag);

                        if (isReadyFlag[0] == 0xFF)
                        {
                            emu.StartGame(smb);
                            emu.CurrentState = Emulator.State.RUNNING;
                        }
                    }

                    if (emu.CurrentState != Emulator.State.RUNNING)
                    {
                        continue;
                    }

                    byte[] coin = new byte[1];
                    byte[] time = new byte[3];
                    emu.ReadGameMemory(0x746, 1, out coin);
                    emu.ReadGameMemory(0x7E0, 3, out time);

                    smb.CurrentCoins = coin[0];
                    smb.CurrentTime = string.Format("{0}{1}{2}", time[0], time[1], time[2]);

                    await Task.Delay(17);
                } while (true);
            });
        }
    }
}
