using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace thearcadearcade.Library
{
    class Manager
    {
        private Dictionary<string, PlatformGameList> gamesPerPlatform = new Dictionary<string, Library.PlatformGameList>();
        public Dictionary<string, PlatformGameList> GamesPerPlatform
        {
            get
            {
                return gamesPerPlatform;
            }
        }

        private Dictionary<string, GameHooks.Emulator> emulators = new Dictionary<string, GameHooks.Emulator>();
        public List<string> AvailablePlatforms
        {
            get
            {
                return emulators.Keys.ToList<string>();
            }
        }
        public GameHooks.Emulator GetEmulatorByPlatform(string platform)
        {
            Debug.Assert(emulators.ContainsKey(platform), "Platform not supported!", "No emulator has been configured for platform '{0}'!", platform);
            return emulators[platform];
        }

        public Manager()
        {
            string[] platformPaths = Directory.GetDirectories(Path.Combine(Directory.GetCurrentDirectory(), "platforms\\"));
            foreach (string platformPath in platformPaths)
            {
                string platformName = Path.GetFileName(platformPath);
                SearchForGames(platformName);
                SearchForEmulator(platformName);
            }
        }

        private void SearchForGames(string platformName)
        {
            gamesPerPlatform[platformName] = new PlatformGameList(platformName);
        }

        private void SearchForEmulator(string platformName)
        {
            string platformInfo = Path.Combine(Directory.GetCurrentDirectory(), "platforms", platformName, "config.json");
            emulators[platformName] = GameHooks.Emulator.FromJSON(platformInfo);
        }
    }
}
