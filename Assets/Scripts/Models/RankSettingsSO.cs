using UnityEngine;

namespace Tanki.Models
{
    [CreateAssetMenu(fileName = "RankSettings", menuName = "Tanki/Rank Settings")]
    public class RankSettingsSO : ScriptableObject
    {
        [Header("Default Ranks")]
        public Sprite[] defaultRanksSmall;
        
        [Header("Premium Ranks")]
        public Sprite[] premiumRanksSmall;

        public Sprite GetRankSprite(int rank, bool isPremium)
        {
            Sprite[] sprites = isPremium ? premiumRanksSmall : defaultRanksSmall;
            
            if (sprites == null || sprites.Length == 0) return null;
            
            // Ranks usually start from 1, so index is rank - 1
            int index = rank - 1;
            if (index < 0) index = 0;
            if (index >= sprites.Length) index = sprites.Length - 1;
            
            return sprites[index];
        }
    }
}
