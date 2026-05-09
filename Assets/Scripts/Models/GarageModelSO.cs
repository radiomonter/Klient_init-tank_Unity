using System.Collections.Generic;
using UnityEngine;
using Tanki.Networking.Data;

namespace Tanki.Models
{
    [CreateAssetMenu(fileName = "Garage Model", menuName = "Tanki/Models/Garage Model")]
    public class GarageModelSO : ScriptableObject
    {
        [SerializeField] private List<GarageItemData> _inventory = new List<GarageItemData>();
        [SerializeField] private List<GarageItemData> _market = new List<GarageItemData>();

        public List<GarageItemData> Inventory => _inventory;
        public List<GarageItemData> Market => _market;

        public void SetInventory(GarageItemData[] items)
        {
            _inventory.Clear();
            _inventory.AddRange(items);
            OnInventoryUpdated?.Invoke();
        }

        public void SetMarket(GarageItemData[] items)
        {
            _market.Clear();
            _market.AddRange(items);
            OnMarketUpdated?.Invoke();
        }

        public event System.Action OnInventoryUpdated;
        public event System.Action OnMarketUpdated;
    }
}
