using System;

namespace Tanki.Networking.Data
{
    [Serializable]
    public class BattleUserData
    {
        public string user;
        public int kills;
        public int score;
        public string suspicious;
    }

    [Serializable]
    public class BattleInfoData
    {
        public string itemId;
        public string battleMode;
        public string name;
        public int preview;
        public int scoreLimit;
        public int timeLimitInSec;
        public int maxPeopleCount;
        public int minRank;
        public int maxRank;
        public bool proBattle;
        public bool parkourMode;
        public bool roundStarted;
        public bool spectator;
        public int timeLeftInSec;
        public bool userPaidNoSuppliesBattle;
        public bool withoutBonuses;
        public bool withoutCrystals;
        public bool withoutSupplies;
        public string equipmentConstraintsMode;
        public bool reArmorEnabled;
        public int proBattleEnterPrice;
        public int proBattleTimeLeftInSec;
        
        public BattleUserData[] users; // DM
        public BattleUserData[] usersBlue; // Team
        public BattleUserData[] usersRed; // Team
        
        public int scoreBlue;
        public int scoreRed;
        public bool autoBalance;
        public bool friendlyFire;
    }
}
