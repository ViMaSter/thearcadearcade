using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace thearcadearcade.GameHooks
{
    public class MemoryArea
    {
        [JsonProperty("memoryRangeStart")]
        int MemoryRangeStart;
        [JsonProperty("memoryRangeLength")]
        int MemoryRangeLength;

        public bool IsValid
        {
            get
            {
                return MemoryRangeStart == -1 || MemoryRangeLength == -1;
            }
        }

        private MemoryArea()
        {
            MemoryRangeStart = -1;
            MemoryRangeLength = -1;
        }

        public static MemoryArea InvalidObject
        {
            get
            {
                return new MemoryArea();
            }
        }

        public MemoryArea(int memoryRangeStart, int memoryRangeLength)
        {
            MemoryRangeStart = memoryRangeStart;
            MemoryRangeLength = memoryRangeLength;
        }

        byte[] GetBytes(Emulator emulatorToUse)
        {
            byte[] buffer = new byte[MemoryRangeLength];
            emulatorToUse.ReadGameMemory(MemoryRangeStart, MemoryRangeLength, out buffer);
            return buffer;
        }

        public byte GetByte(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return buffer[0];
        }
        public bool GetBoolean(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToBoolean(buffer, 0);
        }

        public char GetChar(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToChar(buffer, 0);
        }

        public short GetInt16(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToInt16(buffer, 0);
        }

        public int GetInt32(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToInt32(buffer, 0);
        }

        public long GetInt64(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToInt64(buffer, 0);
        }

        public ushort GetUInt16(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public uint GetUInt32(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public ulong GetUInt64(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public float GetFloat(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double GetDouble(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToDouble(buffer, 0);
        }

        public string GetString(Emulator emulatorToUse)
        {
            byte[] buffer = GetBytes(emulatorToUse);
            return BitConverter.ToString(buffer, 0);
        }
    }

    public class Game : INotifyPropertyChanged
    {
        internal string Name
        {
            get { return name; }
        }

        [JsonProperty()]
        string name = "";
        internal string Region
        {
            get { return region; }
        }
        [JsonProperty()]
        string region = "";

        internal string Platform
        {
            get { return platform; }
        }
        [JsonProperty()]
        string platform = "";

        internal string Filename
        {
            get { return filename; }
        }
        [JsonProperty()]
        string filename = "";

        public bool IsValid
        {
            get
            {
                return name != "" && region != "" && platform != "" && filename != "";
            }
        }

        [JsonProperty()]
        Dictionary<string, MemoryArea> memoryAreas = new Dictionary<string, MemoryArea>();
        public Dictionary<string, MemoryArea> GetAllMemoryAreas()
        {
            return memoryAreas;
        }
        public MemoryArea GetMemoryArea(string key)
        {
            if (memoryAreas.ContainsKey(key))
            {
                return memoryAreas[key];
            }
            return MemoryArea.InvalidObject;
        }
        void AddMemoryArea(string key, MemoryArea memoryArea)
        {
            memoryAreas.Add(key, memoryArea);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public static Game FromJSON(string pathToJSONFile)
        {
            string fileContents = File.ReadAllText(pathToJSONFile);
            return JsonConvert.DeserializeObject<Game>(fileContents);
        }
    }
}
