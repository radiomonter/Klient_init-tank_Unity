using System.Collections.Generic;
using UnityEngine;
using Tanki.Models;
using Tanki.Networking.Data;
using Tanki.Networking;

namespace Tanki.UI
{
    public class BattleListController : MonoBehaviour
    {
        [Header("Data References")]
        [SerializeField] private BattleListSO _battleList;
        [SerializeField] private NetworkClient _network;

        [Header("UI References")]
        [SerializeField] private Transform _container;
        [SerializeField] private BattleItemUI _itemPrefab;

        private List<BattleItemUI> _instantiatedItems = new List<BattleItemUI>();

        private void OnEnable()
        {
            if (_battleList != null)
            {
                _battleList.OnListUpdated += RefreshList;
                RefreshList();
            }
        }

        private void OnDisable()
        {
            if (_battleList != null)
            {
                _battleList.OnListUpdated -= RefreshList;
            }
        }

        public void RefreshList()
        {
            // Clear existing items
            foreach (var item in _instantiatedItems)
            {
                Destroy(item.gameObject);
            }
            _instantiatedItems.Clear();

            // Populate list
            if (_battleList != null && _battleList.Battles != null)
            {
                foreach (var battle in _battleList.Battles)
                {
                    var item = Instantiate(_itemPrefab, _container);
                    item.Initialize(battle, OnBattleSelected);
                    _instantiatedItems.Add(item);
                }
            }
        }

        private void OnBattleSelected(BattleItemData battle)
        {
            if (battle == null || _network == null) return;
            
            Debug.Log($"[BattleList] Selected battle: {battle.name} ({battle.battleId})");
            
            // Send select command to server
            _network.Send(ProtocolConstants.CommandTypes.Lobby, "select_battle", battle.battleId);
        }
    }
}
