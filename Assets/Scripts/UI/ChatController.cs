using UnityEngine;
using UnityEngine.UI;
using Tanki.Networking;
using System.Collections.Generic;

namespace Tanki.UI
{
    public class ChatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkClient _network;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Text _chatDisplayText;
        [SerializeField] private InputField _chatInputField;
        [SerializeField] private Button _sendButton;

        private void OnEnable()
        {
            if (_chatInputField != null) _chatInputField.onEndEdit.AddListener(OnInputEndEdit);
            if (_sendButton != null) _sendButton.onClick.AddListener(SendMessage);
        }

        private void OnDisable()
        {
            if (_chatInputField != null) _chatInputField.onEndEdit.RemoveListener(OnInputEndEdit);
            if (_sendButton != null) _sendButton.onClick.RemoveListener(SendMessage);
        }

        private void OnInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendMessage();
            }
        }

        public void SendMessage()
        {
            string message = _chatInputField.text;
            if (string.IsNullOrWhiteSpace(message)) return;

            Debug.Log($"[Chat] Sending message: {message}");
            _network.Send(ProtocolConstants.CommandTypes.LobbyChat, "send_message", message);
            
            _chatInputField.text = "";
            _chatInputField.ActivateInputField();
        }

        public void AddMessage(string sender, string message, bool isSystem = false)
        {
            string formattedMessage = isSystem 
                ? $"<color=yellow>{message}</color>\n" 
                : $"<color=green>{sender}:</color> {message}\n";

            _chatDisplayText.text += formattedMessage;
            
            // Auto scroll to bottom
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        public void ClearChat()
        {
            _chatDisplayText.text = "";
        }
    }
}
