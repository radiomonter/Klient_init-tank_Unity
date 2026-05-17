using UnityEngine;
using UnityEngine.UI;
using Tanki.Networking;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tanki.UI
{
    [Serializable]
    public class SavedAccount
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class SavedAccountList
    {
        public List<SavedAccount> accounts = new List<SavedAccount>();
    }

    public class EntranceUIController : MonoBehaviour
    {

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
        public Button dropdownButton;

        [Header("Accounts List")]
        public GameObject accountsPanel;
        public Transform accountsContainer;
        public GameObject accountItemPrefab;

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

        [Header("Status & Errors")]
        public Text statusText;
        public GameObject connectionBlockerPanel;

        private SavedAccountList _savedAccounts = new SavedAccountList();
        private const string ACCOUNTS_KEY = "SavedAccountsList";

        private void Start()
        {
            // Проверка на наличие всех компонентов
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
            if (toRegistrationButton != null) toRegistrationButton.onClick.AddListener(() => SwitchView(1));
            if (toLoginButton != null) toLoginButton.onClick.AddListener(() => SwitchView(0));
            if (forgotPasswordButton != null) forgotPasswordButton.onClick.AddListener(() => SwitchView(2));
            if (cancelRestoreButton != null) cancelRestoreButton.onClick.AddListener(() => SwitchView(0));
            
            if (dropdownButton != null) dropdownButton.onClick.AddListener(ToggleAccountsList);

            // По умолчанию показываем вход
            SwitchView(0);

            // Загрузка сохраненных данных
            LoadAccounts();
            
            if (accountsPanel != null) accountsPanel.SetActive(false);

            // Подписываемся на события сети
            var net = NetworkClient.Instance;
            if (net == null) net = FindObjectOfType<NetworkClient>();
            if (net != null)
            {
                net.OnConnectionSuccess += HandleConnectionSuccess;
                net.OnConnectionError += HandleConnectionError;

                if (net.State == NetworkClient.ConnectionState.Connected)
                {
                    HandleConnectionSuccess();
                }
                else if (net.State == NetworkClient.ConnectionState.Error)
                {
                    HandleConnectionError(net.LastError);
                }
                else
                {
                    if (statusText != null) statusText.text = $"Подключение к серверу {NetworkConfig.Host}:{NetworkConfig.Port}...";
                    if (connectionBlockerPanel != null) connectionBlockerPanel.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net != null)
            {
                net.OnConnectionSuccess -= HandleConnectionSuccess;
                net.OnConnectionError -= HandleConnectionError;
            }
        }

        private void HandleConnectionSuccess()
        {
            if (statusText != null) statusText.text = "";
            if (connectionBlockerPanel != null) connectionBlockerPanel.SetActive(false);
        }

        private void HandleConnectionError(string errorMsg)
        {
            Debug.Log($"[EntranceUI] HandleConnectionError called! statusText exists: {statusText != null}");
            if (statusText != null) statusText.text = $"Сервер {NetworkConfig.Host}:{NetworkConfig.Port} недоступен.\n" + errorMsg;
            // Можно оставить connectionBlockerPanel активным, чтобы нельзя было вводить данные
        }

        private void Update()
        {
            // Навигация Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                GameObject current = EventSystem.current.currentSelectedGameObject;
                if (current == null) return;

                InputField next = null;
                if (loginView.activeSelf)
                {
                    if (current == loginUsername.gameObject) next = loginPassword;
                    else if (current == loginPassword.gameObject) next = loginUsername;
                }
                else if (registrationView.activeSelf)
                {
                    if (current == regUsername.gameObject) next = regPassword;
                    else if (current == regPassword.gameObject) next = regConfirmPassword;
                    else if (current == regConfirmPassword.gameObject) next = regEmail;
                    else if (current == regEmail.gameObject) next = regUsername;
                }

                if (next != null) next.ActivateInputField();
            }

            // Отправка по Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (loginView.activeSelf) OnLoginClicked();
                else if (registrationView.activeSelf) OnRegisterClicked();
            }
            
            // Скрытие списка аккаунтов при клике вне него
            if (accountsPanel != null && accountsPanel.activeSelf && Input.GetMouseButtonDown(0))
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(accountsPanel.GetComponent<RectTransform>(), Input.mousePosition) &&
                    !RectTransformUtility.RectangleContainsScreenPoint(dropdownButton.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    accountsPanel.SetActive(false);
                }
            }
        }

        private void LoadAccounts()
        {
            if (PlayerPrefs.HasKey(ACCOUNTS_KEY))
            {
                string json = PlayerPrefs.GetString(ACCOUNTS_KEY);
                Debug.Log($"[Auth] Loading accounts from JSON: {json}");
                _savedAccounts = JsonUtility.FromJson<SavedAccountList>(json);
            }
            else
            {
                Debug.Log("[Auth] No multi-account list found, checking for legacy single account...");
                // Совместимость со старой версией (один аккаунт)
                if (PlayerPrefs.HasKey("SavedUsername"))
                {
                    string oldUser = PlayerPrefs.GetString("SavedUsername");
                    string oldPass = PlayerPrefs.GetString("SavedPassword");
                    _savedAccounts.accounts.Add(new SavedAccount { 
                        username = oldUser,
                        password = oldPass
                    });
                    Debug.Log($"[Auth] Migrated legacy account: {oldUser}");
                    SaveAccounts();
                    PlayerPrefs.DeleteKey("SavedUsername");
                    PlayerPrefs.DeleteKey("SavedPassword");
                }
            }

            Debug.Log($"[Auth] Total saved accounts: {_savedAccounts.accounts.Count}");

            if (_savedAccounts.accounts.Count > 0)
            {
                var last = _savedAccounts.accounts.Last();
                loginUsername.text = last.username;
                loginPassword.text = last.password;
                if (rememberMe != null) rememberMe.isOn = true;
            }
        }

        private void SaveAccounts()
        {
            string json = JsonUtility.ToJson(_savedAccounts);
            Debug.Log($"[Auth] Saving accounts to JSON: {json}");
            PlayerPrefs.SetString(ACCOUNTS_KEY, json);
            PlayerPrefs.Save();
        }

        public void ToggleAccountsList()
        {
            if (accountsPanel == null) { Debug.LogError("[UI] AccountsPanel is missing!"); return; }
            
            bool newState = !accountsPanel.activeSelf;
            accountsPanel.SetActive(newState);
            
            Debug.Log($"[UI] Toggle accounts list: {newState}");
            if (newState)
            {
                PopulateAccountsList();
            }
        }

        private void PopulateAccountsList()
        {
            if (accountsContainer == null || accountItemPrefab == null) 
            {
                Debug.LogError($"[UI] Populate failed: Container={accountsContainer != null}, Prefab={accountItemPrefab != null}");
                return;
            }

            Debug.Log($"[UI] Populating list with {_savedAccounts.accounts.Count} items.");

            // Очистка
            foreach (Transform child in accountsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создание элементов
            foreach (var acc in _savedAccounts.accounts)
            {
                GameObject item = Instantiate(accountItemPrefab, accountsContainer);
                item.SetActive(true);
                item.name = "Account_" + acc.username;
                
                Text nameText = item.GetComponentInChildren<Text>();
                if (nameText != null) nameText.text = acc.username;
                else Debug.LogWarning($"[UI] Text component not found in item for {acc.username}");

                Button selectBtn = item.GetComponent<Button>();
                string uname = acc.username;
                if (selectBtn != null)
                {
                    selectBtn.onClick.AddListener(() => OnAccountSelected(uname));
                }

                // Кнопка удаления
                Button deleteBtn = item.transform.Find("DeleteButton")?.GetComponent<Button>();
                if (deleteBtn != null)
                {
                    deleteBtn.onClick.AddListener(() => {
                        Debug.Log($"[Auth] Deleting account: {uname}");
                        DeleteAccount(uname);
                        PopulateAccountsList();
                    });
                }
                else Debug.LogWarning($"[UI] DeleteButton not found in item for {acc.username}");
            }
        }

        private void OnAccountSelected(string username)
        {
            var acc = _savedAccounts.accounts.FirstOrDefault(a => a.username == username);
            if (acc != null)
            {
                loginUsername.text = acc.username;
                loginPassword.text = acc.password;
                if (rememberMe != null) rememberMe.isOn = true;
            }
            accountsPanel.SetActive(false);
        }

        private void DeleteAccount(string username)
        {
            _savedAccounts.accounts.RemoveAll(a => a.username == username);
            SaveAccounts();
        }

        public void Show(string bgResourceId)
        {
            SetVisible(true);
            Debug.Log($"[UI] Entrance shown with background resource: {bgResourceId}");
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            // Также включаем родительский Canvas, если он есть
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(visible);
        }

        private void SwitchView(int viewIndex)
        {
            if (loginView != null) loginView.SetActive(viewIndex == 0);
            if (registrationView != null) registrationView.SetActive(viewIndex == 1);
            if (restoreView != null) restoreView.SetActive(viewIndex == 2);
            if (accountsPanel != null) accountsPanel.SetActive(false);
        }

        private void OnLoginClicked()
        {
            Debug.Log("[Auth] Login button clicked.");
            var net = NetworkClient.Instance;
            if (net == null) 
            { 
                Debug.LogWarning("[Auth] NetworkClient.Instance is NULL! Trying to find in scene..."); 
                net = FindObjectOfType<NetworkClient>();
                if (net == null) { Debug.LogError("[Auth] Still no NetworkClient found!"); return; }
            }
            
            string username = loginUsername.text;
            string password = loginPassword.text;
            bool remember = rememberMe != null && rememberMe.isOn;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[UI] Please enter username and password");
                return;
            }

            Debug.Log($"[Auth] Sending login request: {username} (remember: {remember})");
            net.Send("auth", "login", "", remember.ToString().ToLower(), username, password);

            // Сохранение данных
            if (remember)
            {
                // Добавляем или обновляем
                var existing = _savedAccounts.accounts.FirstOrDefault(a => a.username == username);
                if (existing != null)
                {
                    existing.password = password;
                    // Перемещаем в конец (как последний использованный)
                    _savedAccounts.accounts.Remove(existing);
                    _savedAccounts.accounts.Add(existing);
                }
                else
                {
                    _savedAccounts.accounts.Add(new SavedAccount { username = username, password = password });
                }
            }
            else
            {
                // Если не запомнить, удаляем этот аккаунт из списка
                _savedAccounts.accounts.RemoveAll(a => a.username == username);
            }
            SaveAccounts();
        }

        private void OnRegisterClicked()
        {
            var net = NetworkClient.Instance;
            if (net == null) 
            { 
                Debug.LogWarning("[Auth] NetworkClient.Instance is NULL! Trying to find in scene..."); 
                net = FindObjectOfType<NetworkClient>();
                if (net == null) { Debug.LogError("[Auth] Still no NetworkClient found!"); return; }
            }

            string username = regUsername.text;
            string password = regPassword.text;
            string confirm = regConfirmPassword.text;
            string email = regEmail.text;

            if (password != confirm)
            {
                Debug.LogWarning("[UI] Passwords do not match");
                return;
            }

            Debug.Log($"[Auth] Sending registration request: {username}");
            net.Send("registration", "register", username, password, email, "");
        }
    }
}
