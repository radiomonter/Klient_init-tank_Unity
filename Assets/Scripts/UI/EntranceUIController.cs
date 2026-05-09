using UnityEngine;
using UnityEngine.UI;
using Tanki.Networking;
using System;

namespace Tanki.UI
{
    public class EntranceUIController : MonoBehaviour
    {
        [Header("Networking")]
        public NetworkClient network;

        [Header("Views")]
        public GameObject loginView;
        public GameObject registrationView;
        public GameObject restoreView;

        [Header("Login Fields")]
        public InputField loginUsername;
        public InputField loginPassword;
        public Toggle rememberMe;
        public Button loginButton;
        public Button forgotPasswordButton;

        [Header("Registration Fields")]
        public InputField regUsername;
        public InputField regPassword;
        public InputField regConfirmPassword;
        public InputField regEmail;
        public Button registerButton;

        [Header("Restore Fields")]
        public InputField restoreEmail;
        public Button recoverButton;
        public Button cancelRestoreButton;

        [Header("Navigation")]
        public Button toRegistrationButton;
        public Button toLoginButton;

        private void Start()
        {
            // Проверка на наличие всех компонентов
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
            if (toRegistrationButton != null) toRegistrationButton.onClick.AddListener(() => SwitchView(1));
            if (toLoginButton != null) toLoginButton.onClick.AddListener(() => SwitchView(0));
            if (forgotPasswordButton != null) forgotPasswordButton.onClick.AddListener(() => SwitchView(2));
            if (cancelRestoreButton != null) cancelRestoreButton.onClick.AddListener(() => SwitchView(0));

            // По умолчанию показываем вход
            SwitchView(0);
        }

        public void Show(string bgResourceId)
        {
            gameObject.SetActive(true);
            Debug.Log($"[UI] Entrance shown with background resource: {bgResourceId}");
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void SwitchView(int viewIndex)
        {
            if (loginView != null) loginView.SetActive(viewIndex == 0);
            if (registrationView != null) registrationView.SetActive(viewIndex == 1);
            if (restoreView != null) restoreView.SetActive(viewIndex == 2);
        }

        private void OnLoginClicked()
        {
            if (network == null) { Debug.LogError("NetworkClient not linked!"); return; }
            
            string username = loginUsername.text;
            string password = loginPassword.text;
            bool remember = rememberMe != null && rememberMe.isOn;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[UI] Please enter username and password");
                return;
            }

            Debug.Log($"[Auth] Sending login request for {username}");
            network.Send("auth", "login", "", remember.ToString().ToLower(), username, password);
        }

        private void OnRegisterClicked()
        {
            if (network == null) { Debug.LogError("NetworkClient not linked!"); return; }

            string username = regUsername.text;
            string password = regPassword.text;
            string confirm = regConfirmPassword.text;
            string email = regEmail.text;

            if (password != confirm)
            {
                Debug.LogWarning("[UI] Passwords do not match");
                return;
            }

            Debug.Log($"[Auth] Sending registration request for {username}");
            network.Send("registration", "register", username, password, email, "");
        }
    }
}
