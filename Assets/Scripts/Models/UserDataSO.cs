using UnityEngine;
using Tanki.Core.Variables;

namespace Tanki.Models
{
    [CreateAssetMenu(fileName = "User Data", menuName = "Tanki/Models/User Data")]
    public class UserDataSO : ScriptableObject
    {
        public StringVariable Uid;
        public IntVariable Rank;
        public IntVariable Crystals;
        public IntVariable Score;
        public IntVariable NextRankScore;
        public BoolVariable IsPremium;

        public void Initialize(string uid, int rank, int crystals, int score, int nextRankScore)
        {
            if (Uid != null) Uid.SetValue(uid);
            else Debug.LogWarning("UserDataSO: Uid variable is not assigned!");
            
            if (Rank != null) Rank.SetValue(rank);
            if (Crystals != null) Crystals.SetValue(crystals);
            if (Score != null) Score.SetValue(score);
            if (NextRankScore != null) NextRankScore.SetValue(nextRankScore);
        }
    }
}
