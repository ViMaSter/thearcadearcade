using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace thearcadearcade.Library
{

    public class Player
    {
        GameHooks.Emulator emulator;
        GameHooks.Game currentGame;
        GameHooks.Game nextGame;

        Task updateCoroutine;

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
        void OnInitializing()
        {
            currentState = State.READY;
        }
        #endregion
        #region READY
        void OnReady()
        {
            if (emulator.CurrentState == GameHooks.Emulator.State.READY)
            {
                if (nextGame != null)
                {
                    int gameStartStatus = emulator.StartGame(nextGame);
                    if (gameStartStatus == 0)
                    {
                        currentGame = nextGame;
                        currentState = State.RUNNING;
                        nextGame = null;
                    }
                    else
                    {
                        Console.WriteLine("Couldn't launch game {0}: Error code {1}", nextGame.Name, gameStartStatus);
                        currentState = State.ERROR;
                        nextGame = null;
                    }
                }
            }
        }
        #endregion
        #region RUNNING
        void OnRunning()
        {
            // score points
            // wait for finish
            //   move to next challenge
            // kill condition to be met
            //   restart
        }
        #endregion
        #region ERROR
        void OnError()
        {
        }
        #endregion
        #endregion

        public Player(GameHooks.Emulator emulator, GameHooks.Game nextGame)
        {
            this.emulator = emulator;
            this.nextGame = nextGame;

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

        public void Update()
        {
            stateCallbacks[currentState]();
        }
    }
}
