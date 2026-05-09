using System;

namespace Tanki.Networking.Data
{
    [Serializable]
    public class BattleItemData
    {
        public string battleId;
        public string battleMode;
        public string name;
        public int preview;
        public int maxPeople;
        public int minRank;
        public int maxRank;
        public bool privateBattle;
        public bool proBattle;
        public bool parkourMode;
        public int timeLeft;
        public string[] users;
        public string[] usersBlue;
        public string[] usersRed;
    }

    [Serializable]
    public class BattleListResponse
    {
        public BattleItemData[] battles;
    }
}
