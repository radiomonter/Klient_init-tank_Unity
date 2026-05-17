using UnityEngine;
using UnityEngine.UI;
using Tanki.Networking;
using System.Collections.Generic;
using Tanki.Models;

namespace Tanki.UI
{
    public class ChatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Text _chatDisplayText;
        [SerializeField] private InputField _chatInputField;
        [SerializeField] private Button _sendButton;

        [Header("Advanced UI (Optional)")]
        [SerializeField] private RankSettingsSO _rankSettings;
        [SerializeField] private GameObject _messagePrefab;
        [SerializeField] private Transform _messagesContainer;

        private void Start()
        {

            // Try to find container if not set
            if (_messagesContainer == null && _scrollRect != null)
                _messagesContainer = _scrollRect.content;
        }

        private void Awake()
        {
            // If components are not linked, we can try to find them or set them up
            if (_scrollRect == null) _scrollRect = GetComponentInChildren<ScrollRect>();
            if (_chatInputField == null) _chatInputField = GetComponentInChildren<InputField>();
            
            // Ensure Rich Text is enabled for the display text
            if (_chatDisplayText != null)
            {
                _chatDisplayText.supportRichText = true;
                _chatDisplayText.alignment = TextAnchor.LowerLeft;
            }
        }

        private void OnEnable()
        {
            if (_chatInputField != null)
            {
                _chatInputField.onEndEdit.RemoveAllListeners();
                _chatInputField.onEndEdit.AddListener(OnInputEndEdit);
            }
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveAllListeners();
                _sendButton.onClick.AddListener(SendMessage);
            }
            Debug.Log("[Chat] UI Listeners reset and added in OnEnable");

            // Load history from LobbyController
            LoadHistory();
        }

        private void LoadHistory()
        {
            Tanki.Controllers.LobbyController lobby = FindObjectOfType<Tanki.Controllers.LobbyController>();
            if (lobby != null && _messagesContainer != null)
            {
                // Clear current UI if any (to avoid duplicates on toggle)
                ClearChat();
                
                foreach (var msg in lobby.GetMessageHistory())
                {
                    AddMessage(msg.name, msg.message, msg.system, msg.rang, msg.sourceUserPremium);
                }
                Debug.Log("[Chat] History loaded from LobbyController.");
            }
        }

        public void ClearChat()
        {
            if (_messagesContainer != null)
            {
                foreach (Transform child in _messagesContainer)
                    Destroy(child.gameObject);
            }
            if (_chatDisplayText != null) _chatDisplayText.text = "";
        }

        private void OnDisable()
        {
            if (_chatInputField != null) _chatInputField.onEndEdit.RemoveListener(OnInputEndEdit);
            if (_sendButton != null) _sendButton.onClick.RemoveListener(SendMessage);
        }

        private void OnInputEndEdit(string text)
        {
            // Enter key is handled in Update for better control
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!_chatInputField.isFocused)
                {
                    _chatInputField.ActivateInputField();
                    Debug.Log("[Chat] Input field focused via Enter key");
                }
                else
                {
                    SendMessage();
                }
            }
        }

        public void SendMessage()
        {
            string message = _chatInputField.text;
            if (string.IsNullOrWhiteSpace(message)) return;

            Debug.Log($"[Chat] Attempting to send message: {message}");
            if (NetworkClient.Instance == null)
            {
                Debug.LogError("[Chat] NetworkClient.Instance is MISSING!");
                return;
            }
            
            NetworkClient.Instance.Send(ProtocolConstants.CommandTypes.Lobby, "chat_message", "", message);
            
            _chatInputField.text = "";
            _chatInputField.ActivateInputField();
        }

        public void AddMessage(string sender, string message, bool isSystem = false, int rank = 0, bool isPremium = false)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            Debug.Log($"[Chat UI] Adding message from {sender}: {message} (System: {isSystem}, Rank: {rank}, Premium: {isPremium})");

            Color senderColor = Color.green;
            Color messageColor = Color.white;
            if (isSystem || string.IsNullOrEmpty(sender))
            {
                senderColor = new Color(1f, 0.84f, 0f); // Gold
                messageColor = senderColor;
            }

            // Try prefab approach first
            if (_messagePrefab != null && _messagesContainer != null)
            {
                GameObject msgObj = Instantiate(_messagePrefab, _messagesContainer);
                ChatMessageItem item = msgObj.GetComponent<ChatMessageItem>();
                if (item != null)
                {
                    Sprite rankSprite = null;
                    if (_rankSettings != null && rank > 0)
                    {
                        rankSprite = _rankSettings.GetRankSprite(rank, isPremium);
                    }
                    item.SetMessage(rankSprite, sender, message, senderColor, messageColor);
                    
                    if (gameObject.activeInHierarchy)
                        StartCoroutine(ScrollToBottom());
                    return;
                }
                else
                {
                    Debug.LogError("[Chat UI] ChatMessageItem component NOT FOUND on prefab!");
                }
            }
            else
            {
                Debug.LogWarning($"[Chat UI] Prefab-based display skipped: Prefab={_messagePrefab != null}, Container={_messagesContainer != null}");
            }

            // Fallback to text-based approach
            if (_chatDisplayText != null)
            {
                string formattedMessage = "";
                if (isSystem || string.IsNullOrEmpty(sender))
                {
                    formattedMessage = $"<color=#FFD700>{message}</color>\n";
                }
                else
                {
                    formattedMessage = $"<color=#00FF00>{sender}:</color> {message}\n";
                }
                _chatDisplayText.text += formattedMessage;
            }

            StartCoroutine(ScrollToBottom());
        }

        private System.Collections.IEnumerator ScrollToBottom()
        {
            // Wait for 2 frames to ensure all layout groups have finished recalculating
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
            }
        }

    }
}
