using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace thearcadearcade.GameHooks
{
    partial class Game
    {
        public string GameName
        {
            get { return gameName; }
        }
        string gameName;
        public string PlatformName
        {
            get { return platformName; }
        }
        string platformName;

        public Game(string _gameName, string _platformName)
        {
            gameName = _gameName;
            platformName = _platformName;
        }
    }
}
