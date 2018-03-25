using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace thearcadearcade.GameHooks
{
    public partial class Emulator
    {
        private class ConverstionHelper
        {
            private static int GetHexVal(char hex)
            {
                int val = (int)hex;
                return val - (val < 58 ? 48 : 55);
            }

            /// <summary>
            /// IMPORTANT: Only supports uppercase-letters
            /// </summary>
            public static byte[] StringToByteArrayFastest(string hex)
            {
                if (hex.Length % 2 == 1)
                    throw new Exception("The binary key cannot have an odd number of digits");

                byte[] arr = new byte[hex.Length >> 1];

                for (int i = 0; i < hex.Length >> 1; ++i)
                {
                    arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
                }

                return arr;
            }
        }

        enum MemoryOffsetType
        {
            OffsetFromStartOfRegion
        }

        #region Static properties
        private string pathToExecutable = "";
        private string executableName = "";
        public string ExecutableName
        {
            get
            {
                return executableName;
            }
        }

        string platform = "";
        public string Platform
        {
            get
            {
                return platform;
            }
        }


        MemoryOffsetType lookupType;
        byte[] lookupValue;
        int lookupOffset;
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

        int[] currentWindowDimensions = new int[0];
        int[] CurrentWindowDimensions
        {
            get
            {
                return currentWindowDimensions;
            }
            set
            {
                currentWindowDimensions = value;
            }
        }

        int BaseGameMemoryAddress;
        public enum State
        {
            INITIALIZING,
            READY,
            CONNECTING,
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
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Sets up Process- and Process-handle-container
        /// </summary>
        /// <returns>
        /// Return codes:
        /// 0 = success
        /// 1 = couldn't open process (see error message)
        /// </returns>
        void SetupProcessHandle()
        {
            // getting minimum & maximum address
            Helper.WinAPI.SYSTEM_INFO sys_info = new Helper.WinAPI.SYSTEM_INFO();
            Helper.WinAPI.GetSystemInfo(out sys_info);

            ProcessAddressSpan[0] = sys_info.minimumApplicationAddress;
            ProcessAddressSpan[1] = sys_info.maximumApplicationAddress;

            ProcessHandle = Helper.WinAPI.OpenProcess(Helper.WinAPI.PROCESS_QUERY_INFORMATION | Helper.WinAPI.PROCESS_WM_READ, false, CurrentProcess.Id);
            Debug.Assert(ProcessHandle != IntPtr.Zero, "Unable to open process", "Unable to open emulator process. Empty handled returned from OpenProcess(). Are you trying to open a process that was already started?");
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
                int sizeQuery = Helper.WinAPI.VirtualQueryEx(ProcessHandle, currentProcessAddressPtr, out mem_basic_info, 28);

                if (sizeQuery == 0)
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

        int AttachToProcess()
        {
            SetupProcessHandle();

            int baseMemorySetupStatusCode = SetBaseMemory();
            if (baseMemorySetupStatusCode != 0)
            {
                Console.WriteLine("Couldn't find base memory of process; was there an emulator before startup?");
                currentState = State.ERROR;
                return baseMemorySetupStatusCode;
            }

            currentState = State.READY;
            return 0;
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
            if (game.Platform != Platform)
            {
                return 1;
            }

            loadedGame = game;
            currentState = State.CONNECTING;

            // switch (commandLineType)
            //{
            //case Separate:
            if (!StartProcess(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\nes\\games\\", game.Filename)))
            {
                return 2;
            }
            if (!StartProcess(replacementArgument))
            {
                return 3;
            }
            //case Combined:
            //if (!StartProcess(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\nes\\games\\", game.Filename) + " " + replacementArgument))
            //{
            //    return 4;
            //}
            //}

            Task gameLoadedTask = Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(100);
                    byte[] buffer = new byte[1];
                    int readMemorySuccess = ReadGameMemory(0, 1, out buffer);
                    if (readMemorySuccess == 0)
                    {
                        if (buffer[0] != 0xFF)
                        {
                            CurrentWindowDimensions = Helper.WinAPI.WindowsReStyle(CurrentProcess.MainWindowHandle);
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

        public void Start()
        {
            if (!StartProcess(""))
            {
                currentState = State.ERROR;
                Console.WriteLine(string.Format("Error starting emulator process {0}", platform));
                return;
            }
            Task.Run(new Action(async () => {
                await Task.Delay(100); // TODO @VM NTH Magic amount of time to wait after a process started seems unstable; assuming WinAPI fails a better alternative?
                AttachToProcess();
            }));
        }

        ~Emulator()
        {
            StopGame();
        }

        public static Emulator FromJSON(string pathToJSONFile)
        {
            string fileContents = File.ReadAllText(pathToJSONFile);
            JObject emulatorInfo = JObject.Parse(fileContents);

            string pathToExecutable = Path.Combine(Directory.GetCurrentDirectory(), "platforms", emulatorInfo["platform"].Value<string>(), "executable", emulatorInfo["executableName"].Value<string>() + ".exe");

            string productVersion = FileVersionInfo.GetVersionInfo(pathToExecutable).ProductVersion;
            JObject memoryAreaInfo = emulatorInfo.SelectToken("['memoryAreasByVersion']['"+ productVersion + "']") as JObject;

            Debug.Assert(
                memoryAreaInfo != null,
                "Executable version not supported!",
                "Executable version '{0}' is not supported. (Supported versions: {1})",
                productVersion,
                string.Join(", ", memoryAreaInfo.Properties().Select(versionNumber => versionNumber.Name))
            );

            Emulator emulator = new Emulator
            {
                executableName = emulatorInfo["executableName"].Value<string>(),
                platform = emulatorInfo["platform"].Value<string>(),
                pathToExecutable = pathToExecutable,
                lookupType = (MemoryOffsetType)Enum.Parse(typeof(MemoryOffsetType), memoryAreaInfo["type"].Value<string>()),
                lookupValue = ConverstionHelper.StringToByteArrayFastest(memoryAreaInfo["value"].Value<string>()),
                lookupOffset = Int32.Parse(memoryAreaInfo["offset"].Value<string>())
            };

            return emulator;
        }
    }
}
