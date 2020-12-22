using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS2Discord
{
    public class enums
    {
        public enum state
        {
            Disconnected, Connected, Awaiting_Connection
        }
        public state currentState { get; set; }
        public  playing nowPlayingType { get; set; }
        public enum playing
        {
            nothing, PS2, PS1
        }
    }
}
