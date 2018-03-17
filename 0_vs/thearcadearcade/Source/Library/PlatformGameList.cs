using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace thearcadearcade.Library
{
    public class PlatformGameList
    {
        List<GameHooks.Game> games = new List<GameHooks.Game>();
        public List<GameHooks.Game> Games
        {
            get
            {
                return games;
            }
        }

        string platform;
        public string Platform
        {
            get
            {
                return platform;
            }
        }

        public PlatformGameList(string _platform)
        {
            platform = _platform;

            string[] gameConfigs = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "platforms", _platform,"games"), "*.json");
            foreach (string gameConfigFilePath in gameConfigs)
            {
                GameHooks.Game game = GameHooks.Game.FromJSON(gameConfigFilePath);
                if (game.Platform == platform)
                {
                    games.Add(game);
                }
            }
        }

        public GameHooks.Game GetGame(string name, string region)
        {
            return games.Find(game => game.Name == name && game.Region == region);
        }

        public bool HasGame(string name, string region)
        {
            return GetGame(name, region).IsValid;
        }
    }
}
