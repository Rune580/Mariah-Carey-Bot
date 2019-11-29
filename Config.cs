using System;
using System.Collections.Generic;
using System.Text;

namespace All_I_Want_For_Christmas_Bot
{
    public class Config
    {
        public string Prefix { get; private set; }
        public string Token { get; private set; }
        public ulong GuildID { get; private set; }

        public Config(string Prefix, string Token, string GuildID)
        {
            this.Prefix = Prefix;
            this.Token = Token;
            this.GuildID = UInt64.Parse(GuildID);
        }

        public Config()
        {

        }
    }
}
