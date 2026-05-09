using UnityEngine;
using UnityEngine.UI;
using Tanki.Networking.Data;

namespace Tanki.UI
{
    public class BattleItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _modeText;
        [SerializeField] private Text _playersText;
        [SerializeField] private Text _rankText;
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _selectButton;

        private BattleItemData _data;
        private System.Action<BattleItemData> _onSelected;

        public void Initialize(BattleItemData data, System.Action<BattleItemData> onSelected)
        {
            _data = data;
            _onSelected = onSelected;

            _nameText.text = data.name;
            _modeText.text = data.battleMode;
            _playersText.text = $"{GetTotalPlayers()}/{data.maxPeople}";
            _rankText.text = $"{data.minRank}-{data.maxRank}";

            // TODO: Set preview image based on data.preview ID

            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => _onSelected?.Invoke(_data));
        }

        private int GetTotalPlayers()
        {
            if (_data.users != null) return _data.users.Length;
            int count = 0;
            if (_data.usersBlue != null) count += _data.usersBlue.Length;
            if (_data.usersRed != null) count += _data.usersRed.Length;
            return count;
        }
    }
}
