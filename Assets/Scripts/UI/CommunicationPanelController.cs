using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    public class CommunicationPanelController : MonoBehaviour
    {
        [Header("Tabs")]
        public Button newsTab;
        public Button chatTab;

        [Header("Views")]
        public GameObject newsView;
        public GameObject chatView;

        [Header("Icons")]
        public Image newsIcon;
        public Image chatIcon;

        [Header("Dynamic Header")]
        public Text headerTitle;
        public Image headerIcon;

        [Header("Sprites")]
        public Sprite tabLeftActive;
        public Sprite tabCenterActive;
        public Sprite tabRightActive;
        public Sprite tabLeftInactive;
        public Sprite tabCenterInactive;
        public Sprite tabRightInactive;
        public Sprite newsIconSprite;
        public Sprite chatIconSprite;

        private void OnEnable()
        {
            if (newsTab != null)
            {
                newsTab.interactable = true;
                newsTab.onClick.RemoveAllListeners();
                newsTab.onClick.AddListener(() => SwitchToTab(true));
                Debug.Log("[CommunicationPanel] NewsTab listener added and set interactable");
            }
            if (chatTab != null)
            {
                chatTab.interactable = true;
                chatTab.onClick.RemoveAllListeners();
                chatTab.onClick.AddListener(() => SwitchToTab(false));
                Debug.Log("[CommunicationPanel] ChatTab listener added and set interactable");
            }
        }

        private void Start()
        {
            // Initial state
            Debug.Log("[CommunicationPanel] Start - Defaulting to News tab");
            SwitchToTab(true);
        }

        public void SwitchToTab(bool isNews)
        {
            Debug.Log($"[CommunicationPanel] Switching to {(isNews ? "News" : "Chat")} tab");
            if (newsView != null) newsView.SetActive(isNews);
            if (chatView != null) chatView.SetActive(!isNews);
            if (headerTitle != null) headerTitle.text = isNews ? "НОВОСТИ" : "ЧАТ";
            if (headerIcon != null) headerIcon.gameObject.SetActive(false);

            UpdateTabVisual(newsTab, isNews);
            UpdateTabVisual(chatTab, !isNews);
            
            if (newsIcon != null) newsIcon.color = isNews ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            if (chatIcon != null) chatIcon.color = !isNews ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }

        private void UpdateTabVisual(Button btn, bool active)
        {
            if (btn == null) return;
            
            // Text styling
            Text t = btn.transform.Find("Text")?.GetComponent<Text>();
            if (t != null)
            {
                t.color = active ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                t.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }

            // Sprite swapping for 3-part button
            Transform bg = btn.transform.Find("BG");
            if (bg != null)
            {
                Image left = bg.Find("Left")?.GetComponent<Image>();
                Image center = bg.Find("Center")?.GetComponent<Image>();
                Image right = bg.Find("Right")?.GetComponent<Image>();

                if (left != null) left.sprite = active ? tabLeftActive : tabLeftInactive;
                if (center != null) center.sprite = active ? tabCenterActive : tabCenterInactive;
                if (right != null) right.sprite = active ? tabRightActive : tabRightInactive;
            }
        }
    }
}
