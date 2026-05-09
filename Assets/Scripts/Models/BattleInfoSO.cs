using UnityEngine;
using Tanki.Networking.Data;

namespace Tanki.Models
{
    [CreateAssetMenu(fileName = "Battle Info", menuName = "Tanki/Models/Battle Info")]
    public class BattleInfoSO : ScriptableObject
    {
        [SerializeField] private BattleInfoData _data;

        public BattleInfoData Data => _data;

        public void SetData(BattleInfoData data)
        {
            _data = data;
            OnInfoUpdated?.Invoke();
        }

        public event System.Action OnInfoUpdated;
    }
}
