using System;
using System.IO;
using System.Collections.Generic;

namespace thearcadearcade.Library
{
    public class PlatformGameList
    {
        List<GameHooks.Game> games = new List<GameHooks.Game>();
        string platform;
        public string Platform
        {
            get
            {
                return platform;
            }
        }

        public PlatformGameList(string pathToGamesDirectory, string _platform)
        {
            platform = _platform;

            string[] gameConfigs = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), pathToGamesDirectory), "*.json");
            foreach (string gameConfigFilePath in gameConfigs)
            {
                GameHooks.Game game = GameHooks.Game.FromJSON(File.ReadAllText(gameConfigFilePath));
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
