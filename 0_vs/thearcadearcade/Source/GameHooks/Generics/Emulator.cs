using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace thearcadearcade.GameHooks
{
    partial class Emulator
    {
        #region Properties
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
                    Console.WriteLine("Error querying process info: Error code {0}", win32Error);
                    CurrentState = State.ERROR;
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
            process.StartInfo.FileName = platformName;
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
        public int StartGame(Game game)
        {
            if (game.PlatformName != this.PlatformName)
            {
                return 1;
            }

            loadedGame = game.GameName;
            if (!StartProcess(game.GameName))
            {
                return 2;
            }

            return 0;
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
    }
}
