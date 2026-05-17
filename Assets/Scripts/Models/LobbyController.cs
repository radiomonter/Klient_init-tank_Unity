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
        private System.Collections.Generic.List<ChatMessageData> _messageHistory = new System.Collections.Generic.List<ChatMessageData>();
        private string _lastNewsJson;
        private const int MAX_HISTORY = 100;

        public System.Collections.Generic.IEnumerable<ChatMessageData> GetMessageHistory() => _messageHistory;

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
            public bool premium;
        }

        [Serializable]
        public class InitPremiumData
        {
            public int left_time;
            public bool needShowWelcomeAlert;
            public bool needShowNotificationCompletionPremium;
            public int reminderCompletionPremiumTime;
            public bool wasShowAlertForFirstPurchasePremium;
            public bool wasShowReminderCompletionPremium;
        }

        public static LobbyController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            TrySubscribeToNetwork();
            RefreshUIRefs();
        }

        private void TrySubscribeToNetwork()
        {
            if (NetworkClient.Instance != null)
            {
                // Unsubscribe first to avoid double-subscription
                NetworkClient.Instance.OnCommandReceived -= HandleCommand;
                NetworkClient.Instance.OnCommandReceived += HandleCommand;
                Debug.Log("[Lobby] Subscribed to NetworkClient.Instance");
            }
            else
            {
                Debug.LogWarning("[Lobby] NetworkClient.Instance not found during subscription attempt.");
            }
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[Lobby] Scene loaded: {scene.name}. Re-linking UI...");
            TrySubscribeToNetwork();
            RefreshUIRefs();
        }

        public void EchoHistory(Tanki.UI.ChatController targetUI)
        {
            if (targetUI == null) return;
            Debug.Log($"[Lobby] Echoing memory ({_messageHistory.Count} messages) to target UI.");
            foreach (var msg in _messageHistory)
            {
                targetUI.AddMessage(msg.name, msg.message, msg.system, msg.rang, msg.sourceUserPremium);
            }
        }

        public void RefreshUIRefs()
        {
            if (_lobbyUI == null) _lobbyUI = FindObjectOfType<Tanki.UI.LobbyUIController>(true);
            if (_chatUI == null) _chatUI = FindObjectOfType<Tanki.UI.ChatController>(true);
            if (_newsUI == null) _newsUI = FindObjectOfType<Tanki.UI.NewsController>(true);
            
            if (_entranceUI == null) _entranceUI = FindObjectOfType<Tanki.UI.EntranceUIController>(true);
            
            if (_chatUI != null)
            {
                EchoHistory(_chatUI);
            }

            if (_newsUI != null && !string.IsNullOrEmpty(_lastNewsJson))
            {
                _newsUI.SetNewsJson(_lastNewsJson);
            }
        }

        private void OnDisable()
        {
            if (NetworkClient.Instance != null)
                NetworkClient.Instance.OnCommandReceived -= HandleCommand;
        }

        private void HandleCommand(Command cmd)
        {
            Debug.Log($"[Network] Received: {cmd.Type} with {cmd.Arguments.Count} args.");
            switch (cmd.Type)
            {
                case ProtocolConstants.CommandTypes.Lobby:
                case "LobbyChat":
                case "LobbyChat::SendChatMessageClient":
                case "chat":
                case "lobby_chat":
                    HandleLobbyCommand(cmd);
                    HandleChatCommand(cmd);
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
                            NetworkClient.Instance.Send("system", "dependencies_loaded", depId);
                        }
                        else if (subType == "init_registration_model")
                        {
                            string json = cmd.Arguments.Count > 1 ? cmd.Arguments[1] : "{}";
                            
                            if (_entranceUI == null) RefreshUIRefs();

                            if (_entranceUI != null)
                                _entranceUI.Show("122842");
                            else
                                Debug.LogError("[Lobby] CRITICAL: Entrance UI is missing!");
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
                                if (_userData.IsPremium != null) 
                                {
                                    Debug.Log($"[Lobby] Set Premium: {data.premium} for {data.name}");
                                    _userData.IsPremium.SetValue(data.premium);
                                }
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
                
                case "init_premium":
                    if (cmd.Arguments.Count > 1)
                    {
                        try
                        {
                            string premJson = cmd.Arguments[1];
                            Debug.Log($"[Lobby] init_premium data: {premJson}");
                            var premData = JsonUtility.FromJson<InitPremiumData>(premJson);
                            bool hasPremium = premData.left_time > 0;
                            Debug.Log($"[Lobby] Premium status: {hasPremium} (left_time={premData.left_time})");
                            if (_userData != null && _userData.IsPremium != null)
                            {
                                _userData.IsPremium.SetValue(hasPremium);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[Lobby] Error parsing init_premium: {e.Message}");
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


                case "end_layout_switch":
                    Debug.Log($"[Lobby] Layout switch finished. Current view: {cmd.Arguments[1]}");
                    break;

                default:
                    if (!subType.Trim().StartsWith("{") && !subType.Trim().StartsWith("["))
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

        [Serializable]
        public class ChatMessageData
        {
            public string name;
            public string message;
            public bool system;
            public int rang;
            public bool sourceUserPremium;
        }

        [Serializable]
        public class NewsItemData
        {
            public string image;
            public string date;
            public string header;
            public string id;
            public string text;
        }

        [Serializable]
        public class InitMessagesWrapper
        {
            public ChatMessageData[] messages;
            public NewsItemData[] news;
        }

        private void HandleChatCommand(Command cmd)
        {
            string subType = cmd.Arguments.Count > 0 ? cmd.Arguments[0].Trim() : "";
            string jsonCandidate = "";

            // Identify if the command itself or the first argument is JSON
            bool isDirectJson = subType.StartsWith("{") || subType.StartsWith("[");
            
            if (isDirectJson)
            {
                jsonCandidate = subType;
            }
            else if (cmd.Arguments.Count >= 2)
            {
                string arg1 = cmd.Arguments[1];
                string arg2 = cmd.Arguments.Count > 2 ? cmd.Arguments[2] : "";

                if (subType == "chat_message" || subType == "init_messages" || subType == "lobby_chat" || 
                    cmd.Type == "lobby_chat" || cmd.Type == "LobbyChat")
                {
                    if (subType == "init_messages")
                    {
                        if (arg1.Contains("\"messages\"")) jsonCandidate = arg1;
                        else if (arg2.Contains("\"messages\"")) jsonCandidate = arg2;
                        else jsonCandidate = arg1;
                    }
                    else
                    {
                        // Check if Arg1 is JSON
                        if (arg1.StartsWith("{") || arg1.StartsWith("[")) jsonCandidate = arg1;
                        else if (arg2.StartsWith("{") || arg2.StartsWith("[")) jsonCandidate = arg2;
                    }
                }
            }

            // Fallback for system messages
            if (string.IsNullOrEmpty(jsonCandidate) && cmd.Arguments.Count >= 2)
            {
                if (subType == "system" || subType == "system_message" || subType == "SendSystemChatMessageClient")
                {
                    if (_chatUI != null) _chatUI.AddMessage(null, cmd.Arguments[1], true, 0, false);
                    return;
                }
            }

            if (cmd.Arguments.Count > 0)
            {
                string argsStr = string.Join(" | ", cmd.Arguments);
                Debug.Log($"[Chat Debug] Command: {cmd.Type}, SubType: {subType}, Args: {argsStr}");
            }

            if (!string.IsNullOrEmpty(jsonCandidate) && (jsonCandidate.StartsWith("{") || jsonCandidate.StartsWith("[")))
            {
                Debug.Log($"[Chat] Attempting to parse JSON: {jsonCandidate.Substring(0, Math.Min(jsonCandidate.Length, 100))}...");
                try
                {
                    string json = jsonCandidate.Trim();
                    
                    // Support for Flash-style wrapper object {"messages": [...], "news": [...]}
                    if (json.StartsWith("{") && json.Contains("\"messages\""))
                    {
                        // Simple way to extract the array part or just parse everything inside
                        Debug.Log("[Chat] Detected wrapper object. Extracting messages...");
                    }

                    if (json.StartsWith("["))
                    {
                        // Standard array parsing
                        int pos = 0;
                        int count = 0;
                        while ((pos = json.IndexOf("{", pos)) != -1)
                        {
                            int end = FindClosingBrace(json, pos);
                            if (end != -1)
                            {
                                string singleJson = json.Substring(pos, end - pos + 1);
                                try {
                                    ChatMessageData chatDataArray = JsonUtility.FromJson<ChatMessageData>(singleJson);
                                    if (chatDataArray != null && !string.IsNullOrEmpty(chatDataArray.name)) {
                                        AddToHistory(chatDataArray);
                                        count++;
                                        if (_chatUI != null && _chatUI.gameObject.activeInHierarchy)
                                            _chatUI.AddMessage(chatDataArray.name, chatDataArray.message, chatDataArray.system, chatDataArray.rang, chatDataArray.sourceUserPremium);
                                    }
                                } catch (Exception ex) {
                                    Debug.LogWarning($"[Chat] Failed manual parse of array item: {ex.Message}");
                                }
                                pos = end + 1;
                            }
                            else break;
                        }
                        Debug.Log($"[Chat] Successfully parsed {count} messages from array.");
                    }
                    else if (json.StartsWith("{"))
                    {
                        // Check if it's a wrapper or single message
                        if (json.Contains("\"messages\"") && json.Contains("["))
                        {
                            try {
                                InitMessagesWrapper wrapper = JsonUtility.FromJson<InitMessagesWrapper>(json);
                                if (wrapper != null && wrapper.messages != null)
                                {
                                    Debug.Log($"[Chat] Parsed {wrapper.messages.Length} messages from wrapper.");
                                    foreach (var m in wrapper.messages)
                                    {
                                        if (string.IsNullOrEmpty(m.name)) continue;
                                        AddToHistory(m);
                                        if (_chatUI != null && _chatUI.gameObject.activeInHierarchy)
                                            _chatUI.AddMessage(m.name, m.message, m.system, m.rang, m.sourceUserPremium);
                                    }
                                }
                                
                                // Also handle news if present
                                if (wrapper != null && wrapper.news != null && wrapper.news.Length > 0)
                                {
                                    Debug.Log($"[Chat] Parsed {wrapper.news.Length} news items from wrapper.");
                                    _lastNewsJson = json;
                                    if (_newsUI != null) _newsUI.SetNewsJson(_lastNewsJson); 
                                }
                                return;
                            } catch (Exception ex) {
                                Debug.LogWarning($"[Chat] Failed to parse wrapper with JsonUtility: {ex.Message}. Falling back to manual...");
                            }
                        }
                        
                        // Single message fallback
                        ChatMessageData chatDataSingle = JsonUtility.FromJson<ChatMessageData>(json);
                        if (!string.IsNullOrEmpty(chatDataSingle.name)) {
                            Debug.Log($"[Chat] Parsed single message: {chatDataSingle.name}");
                            AddToHistory(chatDataSingle);
                            if (_chatUI != null && _chatUI.gameObject.activeInHierarchy)
                                _chatUI.AddMessage(chatDataSingle.name, chatDataSingle.message, chatDataSingle.system, chatDataSingle.rang, chatDataSingle.sourceUserPremium);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Chat] Error parsing JSON: {e.Message}. Raw: {jsonCandidate}");
                }
            }
            else if (cmd.Arguments.Count >= 3)
            {
                if (subType == "chat_message" || subType == "lobby_chat")
                {
                    ChatMessageData chatDataOld = new ChatMessageData { name = cmd.Arguments[1], message = cmd.Arguments[2], system = false, rang = 0, sourceUserPremium = false };
                    AddToHistory(chatDataOld);
                    if (_chatUI != null && _chatUI.gameObject.activeInHierarchy)
                        _chatUI.AddMessage(chatDataOld.name, chatDataOld.message, chatDataOld.system, chatDataOld.rang, chatDataOld.sourceUserPremium);
                }
            }
            
            // Restore missing branches
            if (cmd.Arguments.Count > 0)
            {
                if (subType == "init_news" || subType == "show_news")
                {
                    if (cmd.Arguments.Count > 1 && _newsUI != null)
                        _newsUI.SetNewsJson(cmd.Arguments[1]);
                }
                else if (subType == "clear_chat" || subType == "clear_all")
                {
                    _messageHistory.Clear();
                    if (_chatUI != null) _chatUI.ClearChat();
                }
                else if (subType == "init_messages")
                {
                    NetworkClient.Instance.Send(ProtocolConstants.CommandTypes.Lobby, "chat_init");
                }
            }
        }

        private void AddToHistory(ChatMessageData data)
        {
            if (data == null) return;
            _messageHistory.Add(data);
            if (_messageHistory.Count > MAX_HISTORY)
                _messageHistory.RemoveAt(0);
            Debug.Log($"[Chat Memory] Added message. History size: {_messageHistory.Count}. Content: {data.message}");
        }
        private int FindClosingBrace(string text, int start)
        {
            int level = 0;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == '{') level++;
                else if (text[i] == '}')
                {
                    level--;
                    if (level == 0) return i;
                }
            }
            return -1;
        }
    }
}
