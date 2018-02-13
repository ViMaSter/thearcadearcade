using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace thearcadearcade.GameHooks
{
    class MemoryArea
    {
        [JsonProperty("memoryRangeStart")]
        int MemoryRangeStart;
        [JsonProperty("memoryRangeLength")]
        int MemoryRangeLength;

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
        string name;
        internal string Region
        {
            get { return region; }
        }
        [JsonProperty()]
        string region;

        internal string Platform
        {
            get { return platform; }
        }
        [JsonProperty()]
        string platform;

        internal string Filename
        {
            get { return filename; }
        }
        [JsonProperty()]
        string filename;

        [JsonProperty()]
        Dictionary<string, MemoryArea> memoryAreas = new Dictionary<string, MemoryArea>();
        Dictionary<string, MemoryArea> GetAllMemoryAreas()
        {
            return memoryAreas;
        }
        void AddMemoryArea(string key, MemoryArea memoryArea)
        {
            memoryAreas.Add(key, memoryArea);
        }

        public Game(string _name, string _region, string _platform)
        {
            name = _name;
            region = _region;
            platform = _platform;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public static Game FromJSON(string jsonString)
        {
            return JsonConvert.DeserializeObject<Game>(jsonString);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
