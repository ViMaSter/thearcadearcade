using System;
using System.Collections.Generic;
using System.Linq;

namespace thearcadearcade.States
{
    class Manager
    {
        private Dictionary<string, Type> availableStatesByName = new Dictionary<string, Type>();
        public List<string> AvailableStatesNames
        {
            get
            {
                return new List<string>(availableStatesByName.Keys);
            }
        }

        Base currentState = null;
        public Manager()
        {
            var type = typeof(Base);
            var availableStates = AppDomain.CurrentDomain.GetAssemblies()
                                      .SelectMany(s => s.GetTypes())
                                      .Where(p => (type.IsAssignableFrom(p) && !p.IsInterface));

            foreach (Type state in availableStates)
            {
                availableStatesByName[state.Name] = state;
            }
        }

        public string GetCurrentStateName()
        {
            return currentState.GetType().Name;
        }

        public void SetState(Base newState)
        {
            if (currentState != null)
            {
                currentState.OnLeave();
            }

            newState.OnEnter();
            currentState = newState;
        }
    }
}
