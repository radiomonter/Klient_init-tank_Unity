using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Tanki.Controllers;

namespace Tanki.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject _lobbyView;
        [SerializeField] private GameObject _garageView;
        [SerializeField] private GameObject _settingsView;

        [Header("Lobby Sub-Panels")]
        [SerializeField] private GameObject _newsPanel;
        [SerializeField] private GameObject _battleListPanel;
        [SerializeField] private GameObject _battleInfoPanel;
        [SerializeField] private GameObject _chatPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button _battlesButton;
        [SerializeField] private Button _garageButton;
        [SerializeField] private Button _settingsButton;

        private void Start()
        {
            if (_battlesButton != null) _battlesButton.onClick.AddListener(() => ShowView("lobby"));
            if (_garageButton != null) _garageButton.onClick.AddListener(() => ShowView("garage"));
            if (_settingsButton != null) _settingsButton.onClick.AddListener(() => ShowView("settings"));

            // Self-register to LobbyController if not linked
            LobbyController lobbyCtrl = Object.FindObjectOfType<LobbyController>();
            if (lobbyCtrl != null)
            {
                var field = typeof(LobbyController).GetField("_lobbyUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && (field.GetValue(lobbyCtrl) == null || (field.GetValue(lobbyCtrl) as LobbyUIController) != this))
                {
                    field.SetValue(lobbyCtrl, this);
                    Debug.Log("[LobbyUI] Self-registered to LobbyController.");
                }

                // Fallback: activate itself and hide entrance if we are already in lobby state
                if (lobbyCtrl.gameObject.activeInHierarchy)
                {
                    var entranceField = typeof(LobbyController).GetField("_entranceUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entranceField != null)
                    {
                        var entrance = entranceField.GetValue(lobbyCtrl) as EntranceUIController;
                        if (entrance != null && entrance.gameObject.activeInHierarchy)
                        {
                            entrance.SetVisible(false);
                            this.SetLobbyActive(true);
                        }
                    }
                }
            }

            ShowView("lobby");
        }

        public void ShowView(string viewName)
        {
            bool isLobby = viewName == "lobby";
            bool isGarage = viewName == "garage";
            bool isSettings = viewName == "settings";

            if (_lobbyView != null) _lobbyView.SetActive(isLobby);
            if (_garageView != null) _garageView.SetActive(isGarage);
            if (_settingsView != null) _settingsView.SetActive(isSettings);

            // Sub-panels behavior (Matching legacy client)
            if (_battleListPanel != null) _battleListPanel.SetActive(isLobby);
            if (_battleInfoPanel != null) _battleInfoPanel.SetActive(isLobby);
            
            // Communication panel (News/Chat) usually stays visible in Lobby and Garage
            if (_newsPanel != null) _newsPanel.SetActive(isLobby || isGarage);
            if (_chatPanel != null) _chatPanel.SetActive(isLobby || isGarage);

            Debug.Log($"[LobbyUI] Switched to view: {viewName} (Lobby={isLobby}, Garage={isGarage})");
        }

        public void SetLobbyActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
