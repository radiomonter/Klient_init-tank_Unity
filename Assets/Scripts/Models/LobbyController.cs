using UnityEngine;
using Tanki.Networking;
using Tanki.Networking.Data;
using Tanki.Models;
using System;

namespace Tanki.Controllers
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private NetworkClient _network;
        [SerializeField] private UserDataSO _userData;
        [SerializeField] private GarageModelSO _garageModel;
        [SerializeField] private BattleListSO _battleList;
        [SerializeField] private BattleInfoSO _battleInfo;
        [SerializeField] private Tanki.UI.EntranceUIController _entranceUI;
        [SerializeField] private Tanki.UI.LobbyUIController _lobbyUI;
        [SerializeField] private Tanki.UI.ChatController _chatUI;
        [SerializeField] private Tanki.UI.NewsController _newsUI;
        
        private int _dependencyCounter = 0;

        [Serializable]
        public class InitPanelData
        {
            public string name;
            public int crystall;
            public int rang;
            public int score;
            public int next_score;
            public int currentRankScore;
            public bool hasDoubleCrystal;
        }

        private void OnEnable()
        {
            _network.OnCommandReceived += HandleCommand;
        }

        private void OnDisable()
        {
            _network.OnCommandReceived -= HandleCommand;
        }

        private void HandleCommand(Command cmd)
        {
            Debug.Log($"[Network] Received: {cmd.Type} with {cmd.Arguments.Count} args.");
            switch (cmd.Type)
            {
                case ProtocolConstants.CommandTypes.Lobby:
                    HandleLobbyCommand(cmd);
                    break;
                case ProtocolConstants.CommandTypes.Garage:
                    HandleGarageCommand(cmd);
                    break;
                case ProtocolConstants.CommandTypes.System:
                    if (cmd.Arguments.Count > 0)
                    {
                        string subType = cmd.Arguments[0];
                        if (subType == "load_resources")
                        {
                            _dependencyCounter++;
                            // If the server provides an ID, use it, otherwise use our counter
                            string depId = cmd.Arguments.Count > 1 ? cmd.Arguments[cmd.Arguments.Count - 1] : _dependencyCounter.ToString();
                            Debug.Log($"[Network] Server requested resources (ID: {depId}). Emulating load...");
                            _network.Send("system", "dependencies_loaded", depId);
                        }
                        else if (subType == "init_registration_model")
                        {
                            string json = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : "{}";
                            Debug.Log($"[Network] Init registration model: {json}");
                            if (_entranceUI != null)
                                _entranceUI.Show("122842");
                            else
                                Debug.LogError("[Lobby] Entrance UI is NOT linked in LobbyController!");
                        }
                        else if (subType == "init_locale")
                        {
                            Debug.Log("[Network] Locale initialized");
                        }
                        else if (subType == "main_resources_loaded")
                        {
                            Debug.Log("[Network] Main resources loaded.");
                        }
                    }
                    break;
                case "auth":
                    Debug.Log($"[Auth] Received: {cmd.Arguments[0]}");
                    if (cmd.Arguments[0] == "accept")
                    {
                        Debug.Log("[Auth] Login successful! Switching to Lobby UI.");
                        if (_entranceUI != null) _entranceUI.SetVisible(false);
                        if (_lobbyUI != null) _lobbyUI.SetLobbyActive(true);
                    }
                    break;
                case ProtocolConstants.CommandTypes.Battle:
                    HandleBattleCommand(cmd);
                    break;
                case ProtocolConstants.CommandTypes.LobbyChat:
                    HandleChatCommand(cmd);
                    break;
            }
        }

        private void HandleLobbyCommand(Command cmd)
        {
            if (cmd.Arguments.Count == 0) return;

            string subType = cmd.Arguments[0];
            switch (subType)
            {
                case "init_panel":
                    if (cmd.Arguments.Count > 1)
                    {
                        try
                        {
                            string json = cmd.Arguments[1];
                            Debug.Log($"[Lobby] Panel data: {json}");
                            InitPanelData data = JsonUtility.FromJson<InitPanelData>(json);
                            if (_userData != null)
                            {
                                _userData.Initialize(data.name, data.rang, data.crystall, data.score, data.next_score);
                                if (_lobbyUI != null && !_lobbyUI.gameObject.activeInHierarchy) _lobbyUI.SetLobbyActive(true);
                                if (_entranceUI != null && _entranceUI.gameObject.activeInHierarchy) _entranceUI.SetVisible(false);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[Lobby] Error parsing init_panel: {e.Message}");
                        }
                    }
                    break;
                
                case "init_battle_select":
                    if (cmd.Arguments.Count > 1)
                    {
                        try
                        {
                            string json = cmd.Arguments[1];
                            BattleListResponse response = JsonUtility.FromJson<BattleListResponse>(json);
                            _battleList.SetBattles(response.battles);
                            if (_lobbyUI != null && !_lobbyUI.gameObject.activeInHierarchy) _lobbyUI.SetLobbyActive(true);
                            if (_entranceUI != null && _entranceUI.gameObject.activeInHierarchy) _entranceUI.SetVisible(false);
                            Debug.Log($"[Lobby] Received {response.battles.Length} battles.");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[Lobby] Error parsing init_battle_select: {e.Message}");
                        }
                    }
                    break;

                case "init_messages":
                    Debug.Log("[Chat] Initializing chat parameters...");
                    // Parse chat settings if needed (anti-flood, colors, etc.)
                    break;

                case "init_news":
                case "show_news":
                    if (cmd.Arguments.Count > 1 && _newsUI != null)
                    {
                        _newsUI.SetNewsJson(cmd.Arguments[1]);
                    }
                    break;

                case "end_layout_switch":
                    Debug.Log($"[Lobby] Layout switch finished. Current view: {cmd.Arguments[1]}");
                    break;

                default:
                    Debug.Log($"[Lobby] Unhandled sub-command: {subType}");
                    break;
            }
        }

        private void HandleGarageCommand(Command cmd)
        {
            if (cmd.Arguments.Count == 0) return;

            string action = cmd.Arguments[0];
            switch (action)
            {
                case "init_garage_items":
                    var garage = JsonUtility.FromJson<GarageResponse>(cmd.Arguments[1]);
                    if (_garageModel != null) _garageModel.SetInventory(garage.items);
                    Debug.Log($"[Garage] Loaded {garage.items.Length} inventory items");
                    break;

                case "init_market":
                    var market = JsonUtility.FromJson<GarageResponse>(cmd.Arguments[1]);
                    if (_garageModel != null) _garageModel.SetMarket(market.items);
                    Debug.Log($"[Garage] Loaded {market.items.Length} market items");
                    break;
            }
        }

        private void HandleBattleCommand(Command cmd)
        {
            if (cmd.Arguments.Count < 2) return;

            string action = cmd.Arguments[0];
            if (action == "show_battle_info")
            {
                var info = JsonUtility.FromJson<BattleInfoData>(cmd.Arguments[1]);
                if (_battleInfo != null) _battleInfo.SetData(info);
                Debug.Log($"[Battle] Showing info for: {info.name}");
            }
        }

        private void HandleChatCommand(Command cmd)
        {
            if (cmd.Arguments.Count < 3) return;
            
            // Format: chat_message;sender;message
            string sender = cmd.Arguments[1];
            string message = cmd.Arguments[2];
            
            if (_chatUI != null)
                _chatUI.AddMessage(sender, message);
        }
    }
}
