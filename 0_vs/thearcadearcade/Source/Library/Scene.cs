using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace thearcadearcade.Library
{
    struct MemoryState
    {
        [JsonProperty()]
        private string area;
        public string Area
        {
            get
            {
                return area;
            }
        }

        [JsonProperty()]
        private string comparisonOperator;
        public string ComparisonOperator
        {
            get
            {
                return comparisonOperator;
            }
        }

        [JsonProperty()]
        private string value;
        public string Value
        {
            get
            {
                return value;
            }
        }
    }

    struct EndCondition
    {
        [JsonProperty()]
        private string logicGate;
        public string LogicGate
        {
            get
            {
                return logicGate;
            }
        }

        [JsonProperty()]
        private MemoryState[] memoryState;
        public MemoryState[] MemoryState
        {
            get
            {
                return memoryState;
            }
        }
    }
    struct ScoreDefinition
    {
        [JsonProperty()]
        private string key;
        public string Key
        {
            get
            {
                return key;
            }
        }

        [JsonProperty()]
        private double multiplier;
        public double Multiplier
        {
            get
            {
                return multiplier;
            }
        }
    }
    class Act
    {
        [JsonProperty()]
        private GameHooks.Game game;
        public GameHooks.Game Game
        {
            get
            {
                return game;
            }
        }

        /// <summary>
        /// Fill in additional game data like filename and memory regions
        /// </summary>
        /// <param name="gameData"></param>
        public void FillGameData(GameHooks.Game gameData)
        {
            Debug.Assert(!game.IsValid, "Game data already filled!", "Trying to set information about game {0} ({1}) for {2} which is already fileld!", game.Name, game.Region, game.Platform);
            if (!game.IsValid)
            {
                game = gameData;
            }
        }

        [JsonProperty()]
        private string arguments = "";
        public string Arguments
        {
            get
            {
                return arguments;
            }
        }

        [JsonProperty()]
        private ScoreDefinition score;
        public ScoreDefinition Score
        {
            get
            {
                return score;
            }
        }

        [JsonProperty()]
        private EndCondition[] endConditions;
        public EndCondition[] EndConditions
        {
            get
            {
                return endConditions;
            }
        }
    }

    public class Scene
    {
        // static data
        [JsonProperty()]
        private string name = "";
        public string Name
        {
            get
            {
                return name;
            }
        }

        [JsonProperty()]
        private string description = "";
        public string Description
        {
            get
            {
                return description;
            }
        }

        [JsonProperty()]
        Act[] acts = new Act[0];

        // dynamic data
        public int currentAct = 0;
        public int CurrentAct
        {
            get
            {
                return currentAct;
            }
        }
        Act GetCurrentAct()
        {
            return acts[currentAct];
        }
        /// <summary>
        /// Proceeds to the next act (if there is one)
        /// </summary>
        /// <returns>
        /// Returns either the index of the next act or -1 if no acts are left
        /// </returns>
        public int FinishAct()
        {
            currentAct++;
            if (currentAct >= scoreByAct.Count)
            {
                return -1;
            }
            return currentAct;
        }

        public List<double> scoreByAct = new List<double>();
        public double CurrentScore
        {
            get
            {
                return scoreByAct.Sum();
            }
        }
        public void SetScoreForAct(double score)
        {
            scoreByAct[currentAct] = score;
        }

        public static Scene FromJSON(string jsonString, Dictionary<string, PlatformGameList> gameList)
        {
            Scene scene = JsonConvert.DeserializeObject<Scene>(jsonString);
            foreach (Act act in scene.acts)
            {
                Debug.Assert(gameList.ContainsKey(act.Game.Platform), "Platform not supported!", "Platform {0} used by game {1} ({2}) is not supported!", act.Game.Platform, act.Game.Name, act.Game.Region);
                Debug.Assert(gameList[act.Game.Platform].HasGame(act.Game.Name, act.Game.Region), "Couldn't find game!", "Game {0} ({1}) doesn't exist in the game library!", act.Game.Name, act.Game.Region);
                act.FillGameData(gameList[act.Game.Platform].GetGame(act.Game.Name, act.Game.Region));
            }
            scene.currentAct = 0;
            return scene;
        }

        private Scene() { }
    }
}
