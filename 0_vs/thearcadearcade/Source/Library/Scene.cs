using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace thearcadearcade.Library
{
    public struct MemoryState
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

    public struct Condition
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
        private MemoryState[] memoryStates;
        public MemoryState[] MemoryStates
        {
            get
            {
                return memoryStates;
            }
        }

        public bool IsFulfilled(GameHooks.Game game, GameHooks.Emulator emulator)
        {
            Debug.Assert(memoryStates.Length > 0, "No memory states available!", "IsFulfilled was called, but no memory states are defined! This is undefined behavior!");
            byte value = 0x00;
            foreach (MemoryState memoryState in memoryStates)
            {
                value = game.GetMemoryArea(memoryState.Area).GetByte(emulator);
                bool? statePassed = null;
                switch (memoryState.ComparisonOperator)
                {
                    case "<":
                        statePassed = value < Convert.ToInt32(memoryState.Value);
                        break;
                    case "<=":
                        statePassed = value <= Convert.ToInt32(memoryState.Value);
                        break;
                    case ">":
                        statePassed = value > Convert.ToInt32(memoryState.Value);
                        break;
                    case ">=":
                        statePassed = value >= Convert.ToInt32(memoryState.Value);
                        break;
                    case "==":
                        statePassed = value == Convert.ToInt32(memoryState.Value);
                        break;
                }

                Debug.Assert(statePassed != null,
                    "Invalid comparision operator used; '{0}' is not supported (Comparing field {1} with value {2})",
                    memoryState.ComparisonOperator,
                    memoryState.Area,
                    memoryState.Value
                );

                if (!statePassed.HasValue)
                {
                    break;
                }

                if (logicGate == "OR" && statePassed.Value)
                {
                    return true;
                }

                if (logicGate == "AND" && !(statePassed.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }
    public struct ScoreDefinition
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
    public class Act
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
        private Condition[] loseConditions;
        public Condition[] LoseConditions
        {
            get
            {
                return loseConditions;
            }
        }

        [JsonProperty()]
        private Condition[] winConditions;
        public Condition[] WinConditions
        {
            get
            {
                return winConditions;
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
        private int currentActIndex = 0;
        public int CurrentActIndex
        {
            get
            {
                return currentActIndex;
            }
        }
        public int ActAmount
        {
            get
            {
                return acts.Length;
            }
        }
        public Act CurrentAct
        {
            get
            {
                return acts[currentActIndex];
            }
        }
        /// <summary>
        /// Proceeds to the next act (if there is one)
        /// </summary>
        /// <returns>
        /// Returns either the index of the next act or -1 if no acts are left
        /// </returns>
        public int FinishAct()
        {
            currentActIndex++;
            if (currentActIndex >= acts.Length)
            {
                return -1;
            }
            return currentActIndex;
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
            scene.currentActIndex = 0;
            return scene;
        }

        private Scene() { }
    }
}
