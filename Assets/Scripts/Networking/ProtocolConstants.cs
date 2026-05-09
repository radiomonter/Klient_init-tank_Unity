namespace Tanki.Networking
{
    public static class ProtocolConstants
    {
        public const string CommandDelimiter = "~dne";
        public const string ArgumentDelimiter = ";";
        
        public static class CommandTypes
        {
            public const string Auth = "auth";
            public const string Registration = "registration";
            public const string Chat = "chat";
            public const string Lobby = "lobby";
            public const string Garage = "garage";
            public const string Battle = "battle";
            public const string Ping = "ping";
            public const string LobbyChat = "lobby_chat";
            public const string System = "system";
            public const string Restore = "restore";
            public const string BattleSelect = "battle_select";
            public const string Clan = "clan";
        }
    }
}
