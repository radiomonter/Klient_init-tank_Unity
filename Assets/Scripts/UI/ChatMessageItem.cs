using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    public class ChatMessageItem : MonoBehaviour
    {
        [SerializeField] private Image _rankIcon;
        [SerializeField] private Text _senderText;
        [SerializeField] private Text _messageText;

        public void SetMessage(Sprite rankSprite, string sender, string message, Color senderColor, Color messageColor)
        {
            if (_rankIcon != null)
            {
                if (rankSprite != null)
                {
                    _rankIcon.sprite = rankSprite;
                    _rankIcon.gameObject.SetActive(true);
                }
                else
                {
                    _rankIcon.gameObject.SetActive(false);
                }
            }

            if (_senderText != null)
            {
                _senderText.text = string.IsNullOrEmpty(sender) ? "" : $"{sender}:";
                _senderText.color = senderColor;
                _senderText.gameObject.SetActive(!string.IsNullOrEmpty(sender));
            }

            if (_messageText != null)
            {
                _messageText.text = message;
                _messageText.color = messageColor;
            }
        }
    }
}
