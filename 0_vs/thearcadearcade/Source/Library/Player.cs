using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace thearcadearcade.Library
{
    public class Player
    {
        Scene activeScene;
        GameHooks.Emulator emulator;
        GameHooks.Game nextGame;
        string nextArgument;

        Task updateCoroutine;

        double[] scorePerAct;

        Dictionary<State, Action> stateCallbacks = new Dictionary<State, Action>();

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

        #region State callbacks
        #region INITIALIZING
        private void OnInitializing()
        {
            nextGame = activeScene.CurrentAct.Game;
            nextArgument = activeScene.CurrentAct.Arguments;
            currentState = State.READY;
        }
        #endregion
        #region READY
        private void LaunchNextGame()
        {
            if (nextGame != null)
            {
                int gameStartStatus = emulator.StartGame(nextGame, nextArgument);
                if (gameStartStatus == 0)
                {
                    nextGame = null;
                    nextArgument = null;
                }
                else
                {
                    Console.WriteLine("Couldn't launch game {0}: Error code {1}", nextGame.Name, gameStartStatus);
                    currentState = State.ERROR;
                    nextGame = null;
                }
            }
        }

        private void RestartGame()
        {
            currentState = State.READY;
            int gameStartStatus = emulator.StartGame(activeScene.CurrentAct.Game, activeScene.CurrentAct.Arguments);
            if (gameStartStatus == 0)
            {
            }
            else
            {
                Console.WriteLine("Couldn't restart game {0}: Error code {1}", nextGame.Name, gameStartStatus);
                currentState = State.ERROR;
                nextGame = null;
            }
        }

        private void OnReady()
        {
            if (emulator.CurrentState == GameHooks.Emulator.State.READY)
            {
                LaunchNextGame();
            }
            if (emulator.CurrentState == GameHooks.Emulator.State.RUNNING)
            {
                currentState = State.RUNNING;
            }
        }
        #endregion
        #region RUNNING
        private void UpdateActScore()
        {
            if (emulator.CurrentState == GameHooks.Emulator.State.RUNNING)
            {
                scorePerAct[activeScene.CurrentActIndex] = activeScene.CurrentAct.Game.GetMemoryArea(activeScene.CurrentAct.Score.Key).GetByte(emulator) * activeScene.CurrentAct.Score.Multiplier;
            }
        }

        private bool HasLost()
        {
            foreach (Condition loseCondition in activeScene.CurrentAct.LoseConditions)
            {
                if (loseCondition.IsFulfilled(activeScene.CurrentAct.Game, emulator))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasWon()
        {
            foreach (Condition winCondition in activeScene.CurrentAct.WinConditions)
            {
                if (winCondition.IsFulfilled(activeScene.CurrentAct.Game, emulator))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnRunning()
        {
            UpdateActScore();
            if (HasLost())
            {
                RestartGame();
                return;
                // @TODO: reduce life
            }

            if (HasWon())
            {
                // Try to finish this act...
                if (activeScene.FinishAct() == -1)
                {
                    // ... either there are no acts left
                    Debug.Assert(false, "Game completed!", "Current score: {0}", scorePerAct.Sum());
                    // @TODO: Submit high-score
                }
                else
                {
                    // ... or we proceed to next game
                    nextGame = activeScene.CurrentAct.Game;
                    currentState = State.READY;
                }

                return;
            }
        }
        #endregion
        #region ERROR
        private void OnError()
        {
            Debug.Assert(false);
        }
        #endregion
        #endregion

        public Player(GameHooks.Emulator emulator, Scene scene)
        {
            this.emulator = emulator;
            this.activeScene = scene;
            this.scorePerAct = new double[scene.ActAmount];

            stateCallbacks[State.INITIALIZING] = delegate ()
            {
                OnInitializing();
            };
            stateCallbacks[State.READY] = delegate ()
            {
                OnReady();
            };
            stateCallbacks[State.RUNNING] = delegate ()
            {
                OnRunning();
            };
            stateCallbacks[State.ERROR] = delegate ()
            {
                OnError();
            };

            updateCoroutine = Task.Run(async () => {
                do
                {
                    Update();
                    await Task.Delay(17);
                } while (true);
            });
        }

        private void Update()
        {
            stateCallbacks[currentState]();
        }
    }
}
