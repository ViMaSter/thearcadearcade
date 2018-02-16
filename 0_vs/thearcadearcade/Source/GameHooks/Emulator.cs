using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace thearcadearcade.GameHooks
{
    public partial class Emulator
    {
        #region Static properties
        string platform;
        public string Platform
        {
            get { return platform; }
        }

        string pathToExecutable;
        public string PathToExecutable
        {
            get { return pathToExecutable; }
        }
        #endregion

        #region Dynamic properties
        Game loadedGame;
        public Game LoadedGame
        {
            get { return loadedGame; }
        }

        Process CurrentProcess;
        IntPtr ProcessHandle;
        UIntPtr[] ProcessAddressSpan = new UIntPtr[2];

        int BaseGameMemoryAddress;
        public enum State
        {
            INITIALIZING,
            READY,
            CONNECTING,
            RUNNING,
            ERROR
        };

        State currentState;
        public State CurrentState
        {
            get
            {
                return currentState;
            }
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Sets up Process- and Process-handle-container
        /// </summary>
        void SetupProcessHandle()
        {
            // getting minimum & maximum address
            Helper.WinAPI.SYSTEM_INFO sys_info = new Helper.WinAPI.SYSTEM_INFO();
            Helper.WinAPI.GetSystemInfo(out sys_info);

            ProcessAddressSpan[0] = sys_info.minimumApplicationAddress;
            ProcessAddressSpan[1] = sys_info.maximumApplicationAddress;

            ProcessHandle = Helper.WinAPI.OpenProcess(Helper.WinAPI.PROCESS_QUERY_INFORMATION | Helper.WinAPI.PROCESS_WM_READ, false, CurrentProcess.Id);
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
            Helper.WinAPI.MEMORY_BASIC_INFORMATION mem_basic_info = new Helper.WinAPI.MEMORY_BASIC_INFORMATION();

            int bytesRead = 0;  // number of bytes read with ReadProcessMemory
            for (long currentProcessAddress = (long)ProcessAddressSpan[0]; currentProcessAddress < (long)ProcessAddressSpan[1]; currentProcessAddress += mem_basic_info.RegionSize)
            {
                IntPtr currentProcessAddressPtr = new IntPtr(currentProcessAddress);
                // 28 = sizeof(MEMORY_BASIC_INFORMATION)
                Helper.WinAPI.VirtualQueryEx(ProcessHandle, currentProcessAddressPtr, out mem_basic_info, 28);

                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != 0)
                {
                    return 2;
                }

                // if this memory chunk is accessible
                if (mem_basic_info.Protect == Helper.WinAPI.PAGE_READWRITE && mem_basic_info.State == Helper.WinAPI.MEM_COMMIT)
                {
                    byte[] expectedBuffer = new byte[0];
                    GetLookupValue(out expectedBuffer);

                    byte[] buffer = new byte[expectedBuffer.Length];

                    // read everything in the buffer above
                    Helper.WinAPI.ReadProcessMemory((int)ProcessHandle, mem_basic_info.BaseAddress + GetBaseMemoryOffset(), buffer, buffer.Length, ref bytesRead);

                    if (StructuralComparisons.StructuralEqualityComparer.Equals(buffer, expectedBuffer))
                    {
                        BaseGameMemoryAddress = mem_basic_info.BaseAddress + GetBaseMemoryOffset();
                        return 0;
                    }
                }
            }

            return 1;
        }

        bool StartProcess(string commandLineArguments)
        {
            Process process = new Process();
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo.FileName = pathToExecutable;
            process.StartInfo.Arguments = commandLineArguments;
            // resolve emulator name from platform
            try
            {
                if (CurrentProcess == null)
                {
                    CurrentProcess = process;
                }
                return process.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error starting emulator process {0}: {1}", pathToExecutable, e.ToString()));
                return false;
            }
        }

        void TryToAttachToProcess()
        {
            Task task = Task.Run(async () =>
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

                    currentState = State.READY;
                    await Task.Delay(17);
                    break;
                }
                while (true);
            });
        }
        #endregion

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
            Helper.WinAPI.ReadProcessMemory((int)ProcessHandle, BaseGameMemoryAddress + address, buffer, size, ref bytesRead);
            return bytesRead == 0 ? 1 : 0;
        }

        /// <summary>
        /// Start a game using this emulator
        /// </summary>
        /// <param name="game">Game to start</param>
        /// <returns>
        /// Return code:
        /// 0 = success
        /// 1 = Emulator platform and game platform do not match!
        /// 2 = Couldn't (re)start emulator with argument list
        /// </returns>
        public int StartGame(Game game, string replacementArgument)
        {
            if (game.Platform != this.Platform)
            {
                return 1;
            }

            loadedGame = game;
            currentState = State.CONNECTING;
            if (!StartProcess(replacementArgument.Length != 0 ? replacementArgument : Path.Combine(Directory.GetCurrentDirectory(), "platforms\\nes\\games\\", game.Filename)))
            {
                return 2;
            }

            Task gameLoadedTask = Task.Run(async () =>
            {
                do
                {
                    byte[] buffer = new byte[1];
                    int readMemorySuccess = ReadGameMemory(0, 1, out buffer);
                    if (readMemorySuccess == 0)
                    {
                        if (buffer[0] != 0xFF)
                        {
                            currentState = State.RUNNING;
                            break;
                        }
                    }
                    await Task.Delay(17);
                }
                while (true);
            });

            return 0;
        }

        /// <summary>
        /// Try to kill the emulator process and stop execution
        /// </summary>
        /// <returns>
        /// Return code:
        /// 0 = success
        /// 1 = Process isn't running!
        /// </returns>
        public int StopGame()
        {
            if (CurrentProcess.HasExited)
            {
                return 1;
            }

            CurrentProcess.Kill();
            return 0;
        }

        public Emulator(string _platform, string _pathToExecutable)
        {
            currentState = State.INITIALIZING;
            platform = _platform;
            pathToExecutable = _pathToExecutable;

            if (!StartProcess(""))
            {
                currentState = State.ERROR;
                Console.WriteLine(string.Format("Error starting emulator process {0}", platform));
                return;
            }
            TryToAttachToProcess();
        }

        ~Emulator()
        {
            StopGame();
        }
    }
}
