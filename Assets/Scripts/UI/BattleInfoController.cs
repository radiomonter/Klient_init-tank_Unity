using UnityEngine;
using UnityEngine.UI;
using Tanki.Models;
using Tanki.Networking.Data;
using Tanki.Networking;

namespace Tanki.UI
{
    public class BattleInfoController : MonoBehaviour
    {
        [Header("Data References")]
        [SerializeField] private BattleInfoSO _battleInfo;
        [SerializeField] private NetworkClient _network;

        [Header("UI Elements")]
        [SerializeField] private Text _battleNameText;
        [SerializeField] private Text _mapNameText;
        [SerializeField] private Text _modeText;
        [SerializeField] private Text _scoreLimitText;
        [SerializeField] private Text _timeLimitText;
        [SerializeField] private Text _rankRangeText;
        [SerializeField] private Image _previewImage;
        
        [Header("Player Lists")]
        [SerializeField] private Transform _playerListContainer; // For DM or left side
        [SerializeField] private Transform _playerListRedContainer; // For Team
        [SerializeField] private GameObject _playerEntryPrefab;

        [Header("Controls")]
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _spectateButton;

        private void OnEnable()
        {
            if (_battleInfo != null)
            {
                _battleInfo.OnInfoUpdated += UpdateUI;
                UpdateUI();
            }

            _joinButton.onClick.AddListener(OnJoinClicked);
            _spectateButton.onClick.AddListener(OnSpectateClicked);
        }

        private void OnDisable()
        {
            if (_battleInfo != null)
            {
                _battleInfo.OnInfoUpdated -= UpdateUI;
            }

            _joinButton.onClick.RemoveListener(OnJoinClicked);
            _spectateButton.onClick.RemoveListener(OnSpectateClicked);
        }

        private void UpdateUI()
        {
            var data = _battleInfo.Data;
            if (data == null || string.IsNullOrEmpty(data.itemId))
            {
                // Optionally hide the panel if no data
                return;
            }

            _battleNameText.text = data.name;
            _modeText.text = data.battleMode;
            _scoreLimitText.text = $"Score: {data.scoreLimit}";
            _timeLimitText.text = $"Time: {data.timeLimitInSec / 60}m";
            _rankRangeText.text = $"Ranks: {data.minRank}-{data.maxRank}";

            RefreshPlayerLists(data);
        }

        private void RefreshPlayerLists(BattleInfoData data)
        {
            // Clear containers
            foreach (Transform child in _playerListContainer) Destroy(child.gameObject);
            if (_playerListRedContainer != null) foreach (Transform child in _playerListRedContainer) Destroy(child.gameObject);

            if (data.users != null) // DM
            {
                foreach (var user in data.users)
                {
                    AddPlayerEntry(_playerListContainer, user);
                }
            }
            else // Team
            {
                if (data.usersBlue != null)
                {
                    foreach (var user in data.usersBlue) AddPlayerEntry(_playerListContainer, user);
                }
                if (data.usersRed != null && _playerListRedContainer != null)
                {
                    foreach (var user in data.usersRed) AddPlayerEntry(_playerListRedContainer, user);
                }
            }
        }

        private void AddPlayerEntry(Transform container, BattleUserData user)
        {
            var entry = Instantiate(_playerEntryPrefab, container);
            // entry.GetComponent<BattlePlayerEntryUI>().Initialize(user);
            // For now, let's just assume the prefab has a Text component
            var text = entry.GetComponentInChildren<Text>();
            if (text != null) text.text = $"{user.user} [{user.kills}/{user.score}]";
        }

        private void OnJoinClicked()
        {
            var data = _battleInfo.Data;
            if (data == null) return;

            Debug.Log($"[BattleInfo] Joining battle: {data.itemId}");
            // lobby;join_battle;battleId;team
            // For DM team is "NONE" or empty? In Flash it depends.
            string team = (data.users != null) ? "NONE" : "BLUE"; // Default to blue for team games for now
            _network.Send(ProtocolConstants.CommandTypes.Lobby, "join_battle", data.itemId, team);
        }

        private void OnSpectateClicked()
        {
            var data = _battleInfo.Data;
            if (data == null) return;

            Debug.Log($"[BattleInfo] Spectating battle: {data.itemId}");
            _network.Send(ProtocolConstants.CommandTypes.Lobby, "spectate_battle", data.itemId);
        }
    }
}
