using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using thearcadearcade.Helper;

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
            currentState = State.READY;
        }
        #endregion
        #region READY
        private void LaunchGame()
        {
            nextGame = activeScene.CurrentAct.Game;
            nextArgument = activeScene.CurrentAct.Arguments;
            currentState = State.READY;
            int gameStartStatus = emulator.StartGame(nextGame, Path.Combine(activeScene.RootPath, nextArgument));
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

        private void OnReady()
        {
            if (emulator.CurrentState == GameHooks.Emulator.State.READY)
            {
                LaunchGame();
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
                currentState = State.READY;
                LaunchGame();
                return;
                // @TODO: reduce life
            }

            if (HasWon())
            {
                // Try to finish this act...
                if (activeScene.FinishAct() == -1)
                {
                    // ... either there are no acts left
                    emulator = null;
                    WinAPI.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                    MessageBox.Show(string.Format("Current score: {0}", scorePerAct.Sum()), "Game completed!", MessageBoxButton.OK);
                    Environment.Exit(1);
                    // @TODO: Submit high-score
                }
                else
                {
                    // ... or we proceed to next game
                    nextGame = activeScene.CurrentAct.Game;
                    currentState = State.READY;
                    LaunchGame();
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
