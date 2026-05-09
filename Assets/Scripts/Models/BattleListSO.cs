using System.Collections.Generic;
using UnityEngine;
using Tanki.Networking.Data;

namespace Tanki.Models
{
    [CreateAssetMenu(fileName = "Battle List", menuName = "Tanki/Models/Battle List")]
    public class BattleListSO : ScriptableObject
    {
        [SerializeField] private List<BattleItemData> _battles = new List<BattleItemData>();

        public List<BattleItemData> Battles => _battles;

        public void SetBattles(BattleItemData[] battles)
        {
            _battles.Clear();
            _battles.AddRange(battles);
            OnListUpdated?.Invoke();
        }

        public void AddBattle(BattleItemData battle)
        {
            _battles.Add(battle);
            OnListUpdated?.Invoke();
        }

        public void RemoveBattle(string battleId)
        {
            _battles.RemoveAll(b => b.battleId == battleId);
            OnListUpdated?.Invoke();
        }

        public event System.Action OnListUpdated;
    }
}
