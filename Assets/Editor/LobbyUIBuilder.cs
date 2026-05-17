using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Tanki.UI;
using Tanki.Networking;
using Tanki.Controllers;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.IO;
using Tanki.Models;
using Tanki.Core.Variables;
using System.Collections.Generic;

namespace Tanki.Editor
{
    public class LobbyUIBuilder : EditorWindow
    {
        private const string ASSET_PATH = "Assets/Textures/UI/images/";

        private static Font GetLegacyFont()
        {
            Font font = Resources.Load<Font>("LegacyRuntime");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        private static void PopulateRankSettings(RankSettingsSO settings)
        {
            if (settings == null) return;

            var defaultSmall = new System.Collections.Generic.List<Sprite>();
            var premiumSmall = new System.Collections.Generic.List<Sprite>();

            // Small Ranks (Default)
            for (int i = 1; i <= 31; i++)
            {
                string name = $"DefaultRanksSmallRank{i:00}.png";
                defaultSmall.Add(EnsureIsSprite("ranks/DefaultRanksSmallRank/" + name));
            }

            // Small Ranks (Premium)
            for (int i = 1; i <= 31; i++)
            {
                string name = $"PremiumRankSmallRank{i:00}.png";
                premiumSmall.Add(EnsureIsSprite("ranks/PremiumRankSmallRank/" + name));
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty dsProp = so.FindProperty("defaultRanksSmall");
            SerializedProperty psProp = so.FindProperty("premiumRanksSmall");

            dsProp.arraySize = defaultSmall.Count;
            for (int i = 0; i < defaultSmall.Count; i++) dsProp.GetArrayElementAtIndex(i).objectReferenceValue = defaultSmall[i];

            psProp.arraySize = premiumSmall.Count;
            for (int i = 0; i < premiumSmall.Count; i++) psProp.GetArrayElementAtIndex(i).objectReferenceValue = premiumSmall[i];

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        private static Sprite EnsureIsSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string fileName = Path.GetFileNameWithoutExtension(name);
            string[] guids = AssetDatabase.FindAssets(fileName + " t:texture");
            string path = "";

            if (guids.Length > 0)
            {
                path = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            else
            {
                path = name.StartsWith("Assets") ? name : ASSET_PATH + name;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                
                // Для ProgressBarCentr обязателен Repeat для корректного тайлинга
                if (fileName.Contains("Centr"))
                {
                    if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
                }
                

                if (changed)
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[LobbyUIBuilder] FAILED to load sprite at path: {path}");
            }
            return sprite;
        }


        [MenuItem("Tanki/UI/Build Lobby UI")]
        public static void BuildUI()
        {
            // В режиме воспроизведения диалоги не отображаются, а просто продолжается процесс быстрого прототипирования.
            bool isPlaying = EditorApplication.isPlaying;

            // Cleanup existing LobbyUI
            GameObject oldCanvas = GameObject.Find("LobbyCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);
            
            GameObject canvasObj = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // Убедитесь, что оно находится выше основных элементов, но, возможно, ниже всплывающих окон.
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            GameObject lobbyUIObj = new GameObject("LobbyUI");
            lobbyUIObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = lobbyUIObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            LobbyUIController controller = lobbyUIObj.AddComponent<LobbyUIController>();

            GameObject globalBg = new GameObject("GlobalBackground");
            globalBg.transform.SetParent(lobbyUIObj.transform, false);
            RectTransform gRect = globalBg.AddComponent<RectTransform>();
            Image gImg = globalBg.AddComponent<Image>();
            
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "BITMAP/bitmapBg.png");
            if (bgSprite != null)
            {
                gImg.sprite = bgSprite;
                gImg.type = Image.Type.Tiled;
                gImg.color = Color.white;
            }
            else
            {
                gImg.color = new Color(0.05f, 0.05f, 0.06f, 1f);
            }
            gImg.raycastTarget = false; // Never block raycasts with the background!
            
            gRect.anchorMin = Vector2.zero; gRect.anchorMax = Vector2.one;
            gRect.offsetMin = Vector2.zero; gRect.offsetMax = Vector2.zero;

            // Ensure UserDataSO is fully linked
            UserDataSO userData = EnsureAsset<UserDataSO>("Assets/Data/User Data.asset");
            if (userData != null && userData.IsPremium == null)
            {
                userData.IsPremium = EnsureAsset<BoolVariable>("Assets/Data/Variables/IsPremium.asset");
                EditorUtility.SetDirty(userData);
            }
            
            GameObject topPanel = BuildTopPanel(lobbyUIObj);

            GameObject mainContent = new GameObject("MainContent");
            mainContent.transform.SetParent(lobbyUIObj.transform, false);
            RectTransform mainRect = mainContent.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero; mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = new Vector2(5, 5);
            mainRect.offsetMax = new Vector2(-5, -75);

            float col1End = 0.28f;
            float col2End = 0.75f;

            GameObject col1 = new GameObject("Column1_Communication");
            col1.transform.SetParent(mainContent.transform, false);
            RectTransform r1 = col1.AddComponent<RectTransform>();
            r1.anchorMin = new Vector2(0, 0); r1.anchorMax = new Vector2(col1End, 1);
            r1.offsetMin = new Vector2(0, 0); r1.offsetMax = new Vector2(-5, 0);

            GameObject communicationPanel = CreateTankWindow(col1, "CommunicationPanel", 0, 0);
            RectTransform commRect = communicationPanel.GetComponent<RectTransform>();
            commRect.anchorMin = Vector2.zero; commRect.anchorMax = Vector2.one;
            commRect.offsetMin = Vector2.zero; commRect.offsetMax = Vector2.zero;
            // Здесь нет заголовка, в качестве области заголовка будут использоваться вкладки.
            
            GameObject col2 = new GameObject("Column2_BattleList");
            col2.transform.SetParent(mainContent.transform, false);
            RectTransform r2 = col2.AddComponent<RectTransform>();
            r2.anchorMin = new Vector2(col1End, 0); r2.anchorMax = new Vector2(col2End, 1);
            r2.offsetMin = new Vector2(5, 0); r2.offsetMax = new Vector2(-5, 0);

            GameObject battleListPanel = CreateTankWindow(col2, "BattleListPanel", 0, 0);
            RectTransform blRect = battleListPanel.GetComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero; blRect.anchorMax = Vector2.one;
            blRect.offsetMin = Vector2.zero; blRect.offsetMax = Vector2.zero;
            AddImageHeader(battleListPanel, "battleListRuHeaderClass.png");

            GameObject col3 = new GameObject("Column3_BattleInfo");
            col3.transform.SetParent(mainContent.transform, false);
            RectTransform r3 = col3.AddComponent<RectTransform>();
            r3.anchorMin = new Vector2(col2End, 0); r3.anchorMax = new Vector2(1, 1);
            r3.offsetMin = new Vector2(5, 0); r3.offsetMax = new Vector2(0, 0);

            GameObject lobbyView = new GameObject("LobbyView");
            lobbyView.transform.SetParent(col3.transform, false);
            RectTransform lvRect = lobbyView.AddComponent<RectTransform>();
            lvRect.anchorMin = Vector2.zero; lvRect.anchorMax = Vector2.one;
            lvRect.offsetMin = Vector2.zero; lvRect.offsetMax = Vector2.zero;

            GameObject battleInfoPanel = CreateTankWindow(lobbyView, "BattleInfoPanel", 0, 0);
            RectTransform biRect = battleInfoPanel.GetComponent<RectTransform>();
            biRect.anchorMin = Vector2.zero; biRect.anchorMax = Vector2.one;
            biRect.offsetMin = Vector2.zero; biRect.offsetMax = Vector2.zero;
            AddImageHeader(battleInfoPanel, "battleInfoRuHeaderClass.png");

            GameObject garageView = new GameObject("GarageView");
            garageView.transform.SetParent(col3.transform, false);
            RectTransform gvRect = garageView.AddComponent<RectTransform>();
            gvRect.anchorMin = Vector2.zero; gvRect.anchorMax = Vector2.one;
            gvRect.offsetMin = Vector2.zero; gvRect.offsetMax = Vector2.zero;
            GameObject garageWin = CreateTankWindow(garageView, "GarageWindow", 0, 0);
            garageWin.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            garageWin.GetComponent<RectTransform>().anchorMax = Vector2.one;
            garageWin.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            garageWin.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            AddImageHeader(garageWin, "yourTankRuHeaderClass.png");
            garageView.SetActive(false);

            GameObject settingsView = new GameObject("SettingsView");
            settingsView.transform.SetParent(mainContent.transform, false);
            RectTransform svRect = settingsView.AddComponent<RectTransform>();
            svRect.anchorMin = Vector2.zero; svRect.anchorMax = Vector2.one;
            svRect.offsetMin = Vector2.zero; svRect.offsetMax = Vector2.zero;
            settingsView.SetActive(false);

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_lobbyView").objectReferenceValue = lobbyView;
            so.FindProperty("_garageView").objectReferenceValue = garageView;
            so.FindProperty("_settingsView").objectReferenceValue = settingsView;

            Transform menu = topPanel.transform.Find("Menu");
            if (menu != null)
            {
                so.FindProperty("_battlesButton").objectReferenceValue = menu.Find("BattlesButton")?.GetComponent<Button>();
                so.FindProperty("_garageButton").objectReferenceValue = menu.Find("GarageButton")?.GetComponent<Button>();
                so.FindProperty("_settingsButton").objectReferenceValue = menu.Find("SettingsButton")?.GetComponent<Button>();
            }
            
            ChatController chatCtrl = SetupCommunicationPanel(communicationPanel);
            SetupBattleList(battleListPanel);
            NewsController newsCtrl = col1.GetComponentInChildren<NewsController>();

            // Setup Garage and Lobby view switching properly
            so.FindProperty("_newsPanel").objectReferenceValue = communicationPanel;
            so.FindProperty("_battleListPanel").objectReferenceValue = battleListPanel;
            so.FindProperty("_battleInfoPanel").objectReferenceValue = battleInfoPanel;
            so.FindProperty("_chatPanel").objectReferenceValue = communicationPanel.transform.Find("ContentArea/ChatView")?.gameObject;

            // Re-link to LobbyController
            LobbyController lobby = Object.FindObjectOfType<LobbyController>(true);
            if (lobby == null)
            {
                GameObject gameCtrl = GameObject.Find("GameController");
                if (gameCtrl == null)
                {
                    gameCtrl = new GameObject("GameController");
                    Undo.RegisterCreatedObjectUndo(gameCtrl, "Create GameController");
                }
                lobby = gameCtrl.GetComponent<LobbyController>() ?? gameCtrl.AddComponent<LobbyController>();
                Debug.Log("[UI Builder] Created/Found LobbyController on root GameController.");
            }

            // Move GameController to root if it's a child by mistake
            if (lobby.transform.parent != null)
            {
                lobby.transform.SetParent(null);
                Debug.Log("[UI Builder] Moved LobbyController to root.");
            }

            if (lobby != null)
            {
                SerializedObject soLobby = new SerializedObject(lobby);
                soLobby.FindProperty("_lobbyUI").objectReferenceValue = controller;
                soLobby.FindProperty("_chatUI").objectReferenceValue = chatCtrl;
                soLobby.FindProperty("_newsUI").objectReferenceValue = newsCtrl;
                
                Debug.Log($"[UI Builder] Linked controllers to LobbyController: Chat={chatCtrl != null}, News={newsCtrl != null}");
                
                if (newsCtrl != null)
                {
                    SerializedObject soNews = new SerializedObject(newsCtrl);
                    soNews.FindProperty("_userData").objectReferenceValue = EnsureAsset<Models.UserDataSO>("Assets/Data/User Data.asset");
                    soNews.ApplyModifiedProperties();
                }
                
                LinkDataModels(soLobby);
                
                var network = Object.FindObjectOfType<NetworkClient>();
                if (network != null) soLobby.FindProperty("_network").objectReferenceValue = network;
                
                soLobby.ApplyModifiedProperties();
                
                // Populate Rank Settings
                RankSettingsSO rankSettings = EnsureAsset<RankSettingsSO>("Assets/Data/RankSettings.asset");
                PopulateRankSettings(rankSettings);
                
                EditorUtility.SetDirty(lobby);
                Debug.Log("[UI Builder] LobbyController and RankSettings populated.");
            }

            if (!isPlaying)
            {
                EditorUtility.SetDirty(controller);
                if (lobby != null) EditorUtility.SetDirty(lobby);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            lobbyUIObj.SetActive(isPlaying); // Автоматическая активация в режиме воспроизведения
            if (!isPlaying) Debug.Log("[Lobby Builder] Lobby UI rebuilt and set to inactive (Authorization priority).");
            else Debug.Log("[Lobby Builder] Lobby UI rebuilt and activated in Play Mode.");
        }

        private static void LinkDataModels(SerializedObject soLobby)
        {
            string dataPath = "Assets/Data";
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);

            var userData = EnsureAsset<UserDataSO>(dataPath + "/User Data.asset");
            var garageModel = EnsureAsset<GarageModelSO>(dataPath + "/Garage Model.asset");
            var battleList = EnsureAsset<BattleListSO>(dataPath + "/Battle List.asset");
            var battleInfo = EnsureAsset<BattleInfoSO>(dataPath + "/Battle Info.asset");

            soLobby.FindProperty("_userData").objectReferenceValue = userData;
            soLobby.FindProperty("_garageModel").objectReferenceValue = garageModel;
            soLobby.FindProperty("_battleList").objectReferenceValue = battleList;
            soLobby.FindProperty("_battleInfo").objectReferenceValue = battleInfo;

            if (userData != null)
            {
                SerializedObject soUser = new SerializedObject(userData);
                soUser.FindProperty("Uid").objectReferenceValue = EnsureAsset<StringVariable>(dataPath + "/Variables/Uid.asset");
                soUser.FindProperty("Rank").objectReferenceValue = EnsureAsset<IntVariable>(dataPath + "/Variables/Rank.asset");
                soUser.FindProperty("Crystals").objectReferenceValue = EnsureAsset<IntVariable>(dataPath + "/Variables/Crystals.asset");
                soUser.FindProperty("Score").objectReferenceValue = EnsureAsset<IntVariable>(dataPath + "/Variables/Score.asset");
                soUser.FindProperty("NextRankScore").objectReferenceValue = EnsureAsset<IntVariable>(dataPath + "/Variables/NextRankScore.asset");
                soUser.FindProperty("IsPremium").objectReferenceValue = EnsureAsset<BoolVariable>(dataPath + "/Variables/IsPremium.asset");
                soUser.ApplyModifiedProperties();
                EditorUtility.SetDirty(userData);
            }
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
            }
            return asset;
        }

        private static GameObject BuildTopPanel(GameObject parent)
        {
            GameObject topPanel = new GameObject("TopPanel");
            topPanel.transform.SetParent(parent.transform, false);
            RectTransform rect = topPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, -30); rect.sizeDelta = new Vector2(0, 60);

            // Rank Icon
            GameObject rankGo = new GameObject("RankIcon");
            rankGo.transform.SetParent(topPanel.transform, false);
            RectTransform rankRect = rankGo.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f); rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.anchoredPosition = new Vector2(35, 0); rankRect.sizeDelta = new Vector2(52, 52);
            Image rankImg = rankGo.AddComponent<Image>();
            rankImg.sprite = EnsureIsSprite("ranks/DefaultRanksBigRank/DefaultRanksBigRank01.png");
            rankImg.preserveAspect = true;

            // === ЕДИНЫЙ СТАТУС-БАР (Опыт + Разделитель + Кристаллы) ===
            GameObject barContainer = new GameObject("MainStatusBar");
            barContainer.transform.SetParent(topPanel.transform, false);
            RectTransform barRt = barContainer.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f); barRt.anchorMax = new Vector2(0.55f, 0.5f);
            barRt.offsetMin = new Vector2(65, -14.5f); barRt.offsetMax = new Vector2(0, 14.5f);

            // 1. Левый край
            CreatePart(barContainer, "Left", "progress bar/ProgressBarLeft.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(7, 29), Image.Type.Simple);
            
            // 2. Секция Опыта
            GameObject expSec = new GameObject("ExpSection");
            expSec.transform.SetParent(barContainer.transform, false);
            RectTransform expRt = expSec.AddComponent<RectTransform>();
            expRt.anchorMin = new Vector2(0, 0); expRt.anchorMax = new Vector2(1, 1);
            expRt.offsetMin = new Vector2(7, 0); expRt.offsetMax = new Vector2(-155, 0); // Не наезжает на левый край

            GameObject expBg = CreatePart(expSec, "BG", "progress bar/ProgressBarCentr.png", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Image.Type.Tiled);
            RectTransform ebgRt = expBg.GetComponent<RectTransform>();
            ebgRt.anchorMin = Vector2.zero; ebgRt.anchorMax = Vector2.one;
            ebgRt.offsetMin = new Vector2(-1, 0); ebgRt.offsetMax = new Vector2(1, 0);
            
            // Заливка опыта (используем сплошной цвет, так как оригинальный фон - это просто цвет)
            GameObject expFillCont = new GameObject("FillContainer", typeof(RectTransform));
            expFillCont.transform.SetParent(expSec.transform, false);
            RectTransform efcRt = expFillCont.GetComponent<RectTransform>();
            expFillCont.AddComponent<RectMask2D>();
            efcRt.anchorMin = Vector2.zero; efcRt.anchorMax = new Vector2(0.5f, 1);
            efcRt.offsetMin = new Vector2(0, 2f); efcRt.offsetMax = new Vector2(0, -2f); // Центрируем по вертикали внутри рамки
            
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(expFillCont.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.35f, 0.85f, 0.05f, 1f); // Ярко-зеленый цвет опыта

            // Текст опыта
            GameObject scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(expSec.transform, false);
            RectTransform scoreRt = scoreGo.AddComponent<RectTransform>();
            scoreRt.anchorMin = Vector2.zero; scoreRt.anchorMax = Vector2.one;
            scoreRt.offsetMin = new Vector2(10, 0); scoreRt.offsetMax = new Vector2(-10, 0);
            Text scoreTxt = scoreGo.AddComponent<Text>();
            scoreTxt.font = GetLegacyFont(); scoreTxt.fontSize = 14; scoreTxt.fontStyle = FontStyle.Bold;
            scoreTxt.color = new Color(0.07f, 1f, 0f); scoreTxt.alignment = TextAnchor.MiddleLeft;
            scoreTxt.text = "10000 / 12300 Сержант Player";

            // 3. РАЗДЕЛИТЕЛЬ (ProgressBarLeftRight)
            CreatePart(barContainer, "Divider", "progress bar/ProgressBarLeftRight.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-145, 0), new Vector2(10, 29), Image.Type.Simple);

            // 4. Секция Кристаллов
            GameObject crySec = new GameObject("CrySection");
            crySec.transform.SetParent(barContainer.transform, false); 
            RectTransform cryRt = crySec.AddComponent<RectTransform>();
            cryRt.anchorMin = new Vector2(1, 0); cryRt.anchorMax = new Vector2(1, 1);
            cryRt.offsetMin = new Vector2(-145, 0); cryRt.offsetMax = new Vector2(-10, 0); // Между разделителем и краем
            
            GameObject cryBg = CreatePart(crySec, "BG", "progress bar/ProgressBarCentr.png", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Image.Type.Tiled);
            RectTransform cbgRt = cryBg.GetComponent<RectTransform>();
            cbgRt.anchorMin = Vector2.zero; cbgRt.anchorMax = Vector2.one;
            cbgRt.offsetMin = new Vector2(-1, 0); cbgRt.offsetMax = new Vector2(1, 0); 
            
            GameObject cryTxtGo = new GameObject("Amount");
            cryTxtGo.transform.SetParent(crySec.transform, false);
            RectTransform ctRt = cryTxtGo.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = new Vector2(5, 0); ctRt.offsetMax = new Vector2(-22, 0);
            Text crysTxt = cryTxtGo.AddComponent<Text>();
            crysTxt.font = GetLegacyFont(); crysTxt.fontSize = 12; crysTxt.color = Color.white;
            crysTxt.alignment = TextAnchor.MiddleRight; crysTxt.text = "1 478 528";
 
            // Иконка кристалла
            GameObject crysIcon = new GameObject("Icon");
            crysIcon.transform.SetParent(crySec.transform, false);
            RectTransform ciRt = crysIcon.AddComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(1, 0.5f); ciRt.anchorMax = new Vector2(1, 0.5f);
            ciRt.anchoredPosition = new Vector2(-12, 0); ciRt.sizeDelta = new Vector2(18, 18);
            Image ciImg = crysIcon.AddComponent<Image>();
            ciImg.sprite = EnsureIsSprite("IconCrystalClass.png");
            ciImg.preserveAspect = true;

            // 5. Правый край
            CreatePart(barContainer, "Right", "progress bar/ProgressBarRight.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(10, 29), Image.Type.Simple);

            // === КНОПКИ ===
            GameObject rightSec = new GameObject("RightSection");
            rightSec.transform.SetParent(topPanel.transform, false);
            RectTransform rsrt = rightSec.AddComponent<RectTransform>();
            rsrt.anchorMin = new Vector2(0.55f, 0.5f); rsrt.anchorMax = new Vector2(1, 0.5f);
            rsrt.offsetMin = new Vector2(10, -20); rsrt.offsetMax = new Vector2(-10, 20);
            HorizontalLayoutGroup hlg = rightSec.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight; hlg.childControlWidth = false; hlg.childControlHeight = false; hlg.spacing = 4;

            CreateTopMenuButton(rightSec, "Shop", "МАГАЗИН", "shopCrystals.png", "amber");
            CreateTopMenuButton(rightSec, "Battles", "БИТВЫ", "startIconClass.png", "green");
            CreateTopMenuButton(rightSec, "Garage", "ГАРАЖ", "weaponIconClass.png", "grey");
            CreateTopMenuButton(rightSec, "Clan", "КЛАН", "clanIconClass.png", "grey");
            
            CreateSmallIconButton(rightSec, "Rating", "topClass.png");
            CreateSmallIconButton(rightSec, "Friends", "friendsGreyClass.png");
            CreateSmallIconButton(rightSec, "Invite", "inviteAFriendRuHeaderClass.png");
            CreateSmallIconButton(rightSec, "Settings", "inventoryIconClass.png");
            CreateSmallIconButton(rightSec, "Fullscreen", "activateFullscreenClass.png");
            CreateSmallIconButton(rightSec, "Help", "helpRuHeaderClass.png");
            CreateSmallIconButton(rightSec, "Exit", "closeButtonClass.png");

            // Controller binding
            TopPanelController controller = topPanel.AddComponent<TopPanelController>();
            SerializedObject soTop = new SerializedObject(controller);
            soTop.FindProperty("_uid").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StringVariable>("Assets/Data/Variables/Uid.asset");
            soTop.FindProperty("_rank").objectReferenceValue = AssetDatabase.LoadAssetAtPath<IntVariable>("Assets/Data/Variables/Rank.asset");
            soTop.FindProperty("_crystals").objectReferenceValue = AssetDatabase.LoadAssetAtPath<IntVariable>("Assets/Data/Variables/Crystals.asset");
            soTop.FindProperty("_score").objectReferenceValue = AssetDatabase.LoadAssetAtPath<IntVariable>("Assets/Data/Variables/Score.asset");
            soTop.FindProperty("_nextRankScore").objectReferenceValue = AssetDatabase.LoadAssetAtPath<IntVariable>("Assets/Data/Variables/NextRankScore.asset");
            soTop.FindProperty("_isPremium").objectReferenceValue = EnsureAsset<BoolVariable>("Assets/Data/Variables/IsPremium.asset");

            soTop.FindProperty("_uidText").objectReferenceValue = scoreTxt;
            soTop.FindProperty("_rankIcon").objectReferenceValue = rankImg;
            soTop.FindProperty("_crystalsText").objectReferenceValue = crysTxt;
            soTop.FindProperty("_scoreText").objectReferenceValue = scoreTxt;
            soTop.FindProperty("_progressFillContainer").objectReferenceValue = efcRt;

            // Load rank sprites
            List<Sprite> rankSprites = new List<Sprite>();
            for (int i = 1; i <= 31; i++) rankSprites.Add(EnsureIsSprite($"ranks/DefaultRanksBigRank/DefaultRanksBigRank{i:00}.png"));
            SerializedProperty spSprites = soTop.FindProperty("_rankSprites");
            spSprites.arraySize = rankSprites.Count;
            for (int i = 0; i < rankSprites.Count; i++) spSprites.GetArrayElementAtIndex(i).objectReferenceValue = rankSprites[i];

            // Load premium rank sprites
            List<Sprite> premiumSprites = new List<Sprite>();
            for (int i = 1; i <= 31; i++) premiumSprites.Add(EnsureIsSprite($"ranks/PremiumRankBigRank/PremiumRankBigRank{i:00}.png"));
            SerializedProperty spPremium = soTop.FindProperty("_premiumRankSprites");
            spPremium.arraySize = premiumSprites.Count;
            for (int i = 0; i < premiumSprites.Count; i++) spPremium.GetArrayElementAtIndex(i).objectReferenceValue = premiumSprites[i];
            
            soTop.ApplyModifiedProperties();
            return topPanel;
        }

        private static ChatController SetupCommunicationPanel(GameObject commPanel)
        {
            Font legacyFont = GetLegacyFont();
            
            // Tabs container
            GameObject tabsGo = new GameObject("Tabs", typeof(HorizontalLayoutGroup));
            tabsGo.transform.SetParent(commPanel.transform, false);
            RectTransform trt = tabsGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.anchoredPosition = new Vector2(0, -35);
            trt.sizeDelta = new Vector2(0, 32);

            HorizontalLayoutGroup thlg = tabsGo.GetComponent<HorizontalLayoutGroup>();
            thlg.padding = new RectOffset(10, 10, 0, 0);
            thlg.spacing = 5;
            thlg.childAlignment = TextAnchor.LowerLeft;
            thlg.childControlWidth = false;

            GameObject newsTabObj = CreateCommTab(tabsGo, "NewsTab", "НОВОСТИ", "newsIconClass.png", true);
            GameObject chatTabObj = CreateCommTab(tabsGo, "ChatTab", "ЧАТ", "chatIconClass.png", false);

            CommunicationPanelController commCtrl = commPanel.AddComponent<CommunicationPanelController>();
            SerializedObject soComm = new SerializedObject(commCtrl);
            soComm.FindProperty("newsTab").objectReferenceValue = newsTabObj.GetComponent<Button>();
            soComm.FindProperty("chatTab").objectReferenceValue = chatTabObj.GetComponent<Button>();
            soComm.FindProperty("newsIcon").objectReferenceValue = newsTabObj.transform.Find("Icon")?.GetComponent<Image>();
            soComm.FindProperty("chatIcon").objectReferenceValue = chatTabObj.transform.Find("Icon")?.GetComponent<Image>();
            
            Text headerTitle = AddHeaderText(commPanel, "НОВОСТИ", "newsIconClass.png");
            soComm.FindProperty("headerTitle").objectReferenceValue = headerTitle;
            soComm.FindProperty("headerIcon").objectReferenceValue = headerTitle.transform.parent.Find("Icon")?.GetComponent<Image>();
            
            soComm.FindProperty("tabLeftActive").objectReferenceValue = EnsureIsSprite("leftDownClass_802.png");
            soComm.FindProperty("tabCenterActive").objectReferenceValue = EnsureIsSprite("middleDownClass.png");
            soComm.FindProperty("tabRightActive").objectReferenceValue = EnsureIsSprite("rightDownClass_951.png");
            soComm.FindProperty("tabLeftInactive").objectReferenceValue = EnsureIsSprite("LEFT_497.png");
            soComm.FindProperty("tabCenterInactive").objectReferenceValue = EnsureIsSprite("CENTER_499.png");
            soComm.FindProperty("tabRightInactive").objectReferenceValue = EnsureIsSprite("RIGHT_498.png");
            
            soComm.FindProperty("newsIconSprite").objectReferenceValue = EnsureIsSprite("newsIconClass.png");
            soComm.FindProperty("chatIconSprite").objectReferenceValue = EnsureIsSprite("chatIconClass.png");
            
            soComm.ApplyModifiedProperties();

            // Content Area
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(commPanel.transform, false);
            RectTransform caRect = contentArea.AddComponent<RectTransform>();
            caRect.anchorMin = Vector2.zero; caRect.anchorMax = Vector2.one;
            caRect.offsetMin = new Vector2(11, 12); caRect.offsetMax = new Vector2(-11, -65); 

            // News View
            GameObject newsView = new GameObject("NewsView", typeof(RectTransform));
            newsView.transform.SetParent(contentArea.transform, false);
            RectTransform nvRect = newsView.GetComponent<RectTransform>();
            nvRect.anchorMin = Vector2.zero; nvRect.anchorMax = Vector2.one;
            nvRect.offsetMin = Vector2.zero; nvRect.offsetMax = Vector2.zero;
            newsView.AddComponent<Image>().color = new Color(0.06f, 0.15f, 0.06f, 1f);

            NewsController newsCtrl = newsView.AddComponent<NewsController>();
            
            // Add Scrollable container for news
            GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(newsView.transform, false);
            RectTransform srt = scrollObj.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(5, 5); srt.offsetMax = new Vector2(-5, -5);
            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            viewport.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            viewport.GetComponent<RectTransform>().anchorMax = Vector2.one;
            viewport.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0,0,0,0.1f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = crt;
            sr.horizontal = false; sr.vertical = true;

            SerializedObject soNews = new SerializedObject(newsCtrl);
            soNews.FindProperty("_userData").objectReferenceValue = EnsureAsset<Models.UserDataSO>("Assets/Data/User Data.asset");
            soNews.FindProperty("_container").objectReferenceValue = content.transform;
            soNews.ApplyModifiedProperties();

            // Chat View
            GameObject chatView = new GameObject("ChatView", typeof(RectTransform));
            chatView.transform.SetParent(contentArea.transform, false);
            chatView.AddComponent<Image>().color = new Color(0.04f, 0.12f, 0.04f, 0.8f);
            RectTransform cvRect = chatView.GetComponent<RectTransform>();
            cvRect.anchorMin = Vector2.zero; cvRect.anchorMax = Vector2.one;
            cvRect.offsetMin = Vector2.zero; cvRect.offsetMax = Vector2.zero;

            // Bind views back to CommunicationPanelController
            soComm.FindProperty("newsView").objectReferenceValue = newsView;
            soComm.FindProperty("chatView").objectReferenceValue = chatView;
            soComm.ApplyModifiedProperties();

            GameObject chatScrollObj = new GameObject("ChatScroll", typeof(RectTransform));
            chatScrollObj.transform.SetParent(chatView.transform, false);
            RectTransform csRect = chatScrollObj.GetComponent<RectTransform>();
            csRect.anchorMin = Vector2.zero; csRect.anchorMax = Vector2.one;
            csRect.offsetMin = new Vector2(10, 46); csRect.offsetMax = new Vector2(-10, -8);
            ScrollRect csScroll = chatScrollObj.AddComponent<ScrollRect>();
            csScroll.horizontal = false; csScroll.vertical = true;
            chatScrollObj.AddComponent<Mask>();
            chatScrollObj.AddComponent<Image>().color = new Color(0,0,0,0.01f);

            GameObject chatContent = new GameObject("Content", typeof(RectTransform));
            chatContent.transform.SetParent(chatScrollObj.transform, false);
            RectTransform chcRect = chatContent.GetComponent<RectTransform>();
            chcRect.anchorMin = new Vector2(0, 1); chcRect.anchorMax = new Vector2(1, 1);
            chcRect.pivot = new Vector2(0.5f, 1); chcRect.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup vlgChat = chatContent.AddComponent<VerticalLayoutGroup>();
            vlgChat.childControlHeight = true; vlgChat.childForceExpandHeight = false;
            vlgChat.childControlWidth = true; vlgChat.childForceExpandWidth = true;
            vlgChat.spacing = 0;
            vlgChat.padding = new RectOffset(10, 5, 0, 0);
            chatContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csScroll.content = chcRect;

            GameObject bottomShelf = new GameObject("BottomShelf", typeof(RectTransform));
            bottomShelf.transform.SetParent(chatView.transform, false);
            RectTransform bsRect = bottomShelf.GetComponent<RectTransform>();
            bsRect.anchorMin = new Vector2(0, 0); bsRect.anchorMax = new Vector2(1, 0);
            bsRect.anchoredPosition = new Vector2(0, 18); bsRect.sizeDelta = new Vector2(-4, 34);
            bottomShelf.AddComponent<Image>().sprite = EnsureIsSprite("shortBackgroundHeaderClass.png");

            GameObject sayBtnObj = CreateLegacyButton(bottomShelf, "SayButton", "Сказать", 80);
            sayBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-45, 0);
            sayBtnObj.GetComponent<RectTransform>().anchorMin = sayBtnObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
            Button sayBtn = sayBtnObj.GetComponent<Button>();

            GameObject inputFieldObj = CreateLegacyInput(bottomShelf, "ChatInputField", new Vector2(-45, 0), width: -145);
            InputField inputField = inputFieldObj.GetComponent<InputField>();

            ChatController chatCtrl = chatView.AddComponent<ChatController>();
            SerializedObject soChat = new SerializedObject(chatCtrl);
            soChat.FindProperty("_scrollRect").objectReferenceValue = csScroll;
            soChat.FindProperty("_chatInputField").objectReferenceValue = inputField;
            soChat.FindProperty("_sendButton").objectReferenceValue = sayBtn;
            soChat.FindProperty("_rankSettings").objectReferenceValue = EnsureAsset<RankSettingsSO>("Assets/Data/RankSettings.asset");
            soChat.FindProperty("_messagePrefab").objectReferenceValue = CreateChatMessagePrefab();
            soChat.FindProperty("_messagesContainer").objectReferenceValue = chatContent.transform;
            soChat.ApplyModifiedProperties();

            SerializedObject soCommLate = new SerializedObject(commCtrl);
            soCommLate.FindProperty("newsView").objectReferenceValue = newsView;
            soCommLate.FindProperty("chatView").objectReferenceValue = chatView;
            soCommLate.ApplyModifiedProperties();

            return chatCtrl;
        }

        private static GameObject CreateLegacyButton(GameObject parent, string name, string label, float width)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 30);
            
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(btnObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            // GreenMediumButtonSkin style
            CreatePart(bg, "Left", "leftUpClass_803.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Right", "rightUpClass_879.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Center", "middleUpClass.png", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 16, 30), Image.Type.Tiled);

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label; t.fontSize = 12; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            Image targetImg = btnObj.AddComponent<Image>();
            targetImg.color = new Color(0, 0, 0, 0); 
            targetImg.raycastTarget = true;
            btnObj.AddComponent<Button>();

            return btnObj;
        }

        private static GameObject CreateLegacyInput(GameObject parent, string name, Vector2 pos, float width)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f); rect.anchorMax = new Vector2(1, 0.5f);
            rect.anchoredPosition = pos; 
            rect.offsetMin = new Vector2(10, -13); 
            rect.offsetMax = new Vector2(width, 13);

            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(obj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            CreatePart(bg, "L", "InputLeft.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(10, 26), Image.Type.Simple);
            CreatePart(bg, "R", "InputRight.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(10, 26), Image.Type.Simple);
            CreatePart(bg, "M", "InputCenter.png", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, 26), Image.Type.Tiled);

            RectTransform mRect = bg.transform.Find("M").GetComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero; mRect.anchorMax = Vector2.one;
            mRect.offsetMin = new Vector2(10, 0); mRect.offsetMax = new Vector2(-10, 0);

            Image targetImg = obj.AddComponent<Image>();
            targetImg.color = new Color(0, 0, 0, 0);
            
            InputField inputField = obj.AddComponent<InputField>();
            inputField.targetGraphic = targetImg;
            inputField.transition = Selectable.Transition.None;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(obj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = GetLegacyFont();
            t.fontSize = 12; t.color = Color.white; t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            
            RectTransform tRect = t.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(8, 0); tRect.offsetMax = new Vector2(-8, 0);
            
            inputField.textComponent = t;
            return obj;
        }

        private static GameObject CreateCommTab(GameObject parent, string name, string label, string iconAsset, bool isActive)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(102, 30);
            
            Image targetImg = btnObj.AddComponent<Image>();
            targetImg.color = new Color(0, 0, 0, 0); 
            targetImg.raycastTarget = true;
            Button btn = btnObj.AddComponent<Button>();

            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(btnObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            string leftAsset = isActive ? "leftDownClass_802.png" : "LEFT_497.png";
            string centerAsset = isActive ? "middleDownClass.png" : "CENTER_499.png";
            string rightAsset = isActive ? "rightDownClass_951.png" : "RIGHT_498.png";

            CreatePart(bg, "Left", leftAsset, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Right", rightAsset, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Center", centerAsset, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 30), Image.Type.Tiled);

            GameObject icoObj = new GameObject("Icon");
            icoObj.transform.SetParent(btnObj.transform, false);
            RectTransform icoRect = icoObj.AddComponent<RectTransform>();
            icoRect.anchorMin = new Vector2(0, 0.5f); icoRect.anchorMax = new Vector2(0, 0.5f);
            icoRect.anchoredPosition = new Vector2(12, 0); icoRect.sizeDelta = new Vector2(16, 16);
            Image icoImg = icoObj.AddComponent<Image>();
            icoImg.sprite = EnsureIsSprite(iconAsset);
            icoImg.preserveAspect = true;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = GetLegacyFont();
            t.text = label; t.fontSize = 11; t.alignment = TextAnchor.MiddleLeft; t.color = Color.white;
            RectTransform tRect = t.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(30, 0); tRect.offsetMax = new Vector2(-5, 0);

            return btnObj;
        }

        private static void SetupBattleList(GameObject panel)
        {
            GameObject findBtnObj = new GameObject("FindBattle");
            findBtnObj.transform.SetParent(panel.transform, false);
            RectTransform fbRect = findBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 1); fbRect.anchorMax = new Vector2(0.5f, 1);
            fbRect.anchoredPosition = new Vector2(0, -18); fbRect.sizeDelta = new Vector2(200, 24);
            Button findBtn = findBtnObj.AddComponent<Button>();
            Image findImg = findBtnObj.AddComponent<Image>();
            findImg.sprite = EnsureIsSprite("normalStateClass.png");
            if (findImg.sprite == null) findImg.color = new Color(0.2f, 0.6f, 0.2f);
            findImg.type = Image.Type.Simple;

            GameObject ftObj = new GameObject("Text");
            ftObj.transform.SetParent(findBtnObj.transform, false);
            RectTransform ftRect = ftObj.AddComponent<RectTransform>();
            Text ft = ftObj.AddComponent<Text>();
            ft.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ft.text = "В БОЙ!"; ft.fontSize = 12; ft.alignment = TextAnchor.MiddleCenter; ft.color = Color.white;
            ftRect.anchorMin = Vector2.zero; ftRect.anchorMax = Vector2.one;

            BattleListController ctrl = panel.AddComponent<BattleListController>();
            
            GameObject scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(panel.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero; sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(10, 10); sRect.offsetMax = new Vector2(-10, -25);
            scrollObj.AddComponent<Image>().color = new Color(0,0,0,0.2f);
            scrollObj.AddComponent<Mask>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollObj.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(10, 10, 0, 0);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.content = cRect; sr.vertical = true; sr.horizontal = false;

            typeof(BattleListController).GetField("_container", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ctrl, content.transform);
        }

        private static void CreateTopMenuButton(GameObject parent, string name, string label, string iconAsset, string color)
        {
            GameObject btnObj = new GameObject(name + "Button");
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(95, 24);
            
            Button btn = btnObj.AddComponent<Button>();
            
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(btnObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            string leftAsset, centerAsset, rightAsset;
            if (color == "green") {
                leftAsset = "leftUpClass_803.png";
                centerAsset = "middleUpClass.png";
                rightAsset = "rightUpClass_879.png";
            } else if (color == "amber") {
                leftAsset = "LEFT_497.png"; centerAsset = "CENTER_499.png"; rightAsset = "RIGHT_498.png";
            } else if (color == "grey") {
                leftAsset = "LEFT_497.png"; centerAsset = "CENTER_499.png"; rightAsset = "RIGHT_498.png";
            } else {
                leftAsset = "LEFT_497.png";
                centerAsset = "CENTER_499.png";
                rightAsset = "RIGHT_498.png";
            }

            CreatePart(bg, "L", leftAsset, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 24), Image.Type.Simple);
            CreatePart(bg, "R", rightAsset, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(8, 24), Image.Type.Simple);
            CreatePart(bg, "M", centerAsset, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, Image.Type.Sliced);
            
            RectTransform mRect = bg.transform.Find("M").GetComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero; mRect.anchorMax = Vector2.one;
            mRect.offsetMin = new Vector2(8, 0); mRect.offsetMax = new Vector2(-8, 0);

            GameObject ico = new GameObject("Icon", typeof(RectTransform));
            ico.transform.SetParent(btnObj.transform, false);
            RectTransform irt = ico.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(12, 0); irt.sizeDelta = new Vector2(16, 16);
            Image iImg = ico.AddComponent<Image>();
            iImg.sprite = EnsureIsSprite(iconAsset);
            iImg.preserveAspect = true;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = GetLegacyFont();
            t.text = label; t.fontSize = 11; t.alignment = TextAnchor.MiddleLeft; t.color = Color.white;
            RectTransform trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(28, 0); trt.offsetMax = new Vector2(-5, 0);
            
            string viewName = name.ToLower();
            btn.onClick.AddListener(() => {
                var lobbyUI = GameObject.FindObjectOfType<LobbyUIController>();
                if (lobbyUI != null) lobbyUI.ShowView(viewName);
            });
        }

        private static GameObject CreateTankWindow(GameObject parent, string name, int w, int h)
        {
            GameObject win = new GameObject(name);
            win.transform.SetParent(parent.transform, false);
            RectTransform rect = win.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);

            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(win.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            Image bgImg = bg.AddComponent<Image>();
            bgImg.raycastTarget = false; // Decorative window background
            Sprite bgSprite = EnsureIsSprite("WindowBGTile.png");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Tiled;
            }
            else bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(5, 5); bgRect.offsetMax = new Vector2(-5, -5);

            float f = 5;
            CreatePart(win, "Top", "window/WindowTop.png", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(w > 0 ? w - 10 : 0, f), Image.Type.Simple);
            CreatePart(win, "Bottom", "window/WindowBottom.png", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(w > 0 ? w - 10 : 0, f), Image.Type.Simple);
            CreatePart(win, "Left", "window/WindowLeft.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(f, h > 0 ? h - 10 : 0), Image.Type.Simple);
            CreatePart(win, "Right", "window/WindowRight.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(f, h > 0 ? h - 10 : 0), Image.Type.Simple);

            if (w == 0 || h == 0)
            {
                win.transform.Find("Top").GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                win.transform.Find("Top").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                win.transform.Find("Top").GetComponent<RectTransform>().offsetMin = new Vector2(5, -5);
                win.transform.Find("Top").GetComponent<RectTransform>().offsetMax = new Vector2(-5, 0);

                win.transform.Find("Bottom").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().offsetMin = new Vector2(5, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().offsetMax = new Vector2(-5, 5);

                win.transform.Find("Left").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                win.transform.Find("Left").GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                win.transform.Find("Left").GetComponent<RectTransform>().offsetMin = new Vector2(0, 5);
                win.transform.Find("Left").GetComponent<RectTransform>().offsetMax = new Vector2(5, -5);

                win.transform.Find("Right").GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                win.transform.Find("Right").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            }

            CreatePart(win, "TL", "window/LeftUPСorner.png", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "TR", "window/RightUPСorner.png", new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BR", "window/RightDownСorner.png", new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BL", "window/LeftDownСorner.png", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(f, f), Image.Type.Simple, 0);

            return win;
        }

        private static void CreateSmallIconButton(GameObject parent, string name, string iconAsset)
        {
            GameObject btn = new GameObject(name, typeof(RectTransform));
            btn.transform.SetParent(parent.transform, false);
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
            
            // 3-part background (using same as tabs but small)
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(btn.transform, false);
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

            // Simplified: single image for small buttons if parts are too complex
            Image img = bg.AddComponent<Image>();
            img.sprite = EnsureIsSprite("LEFT_497.png"); // Using tab left part as button base
            img.type = Image.Type.Sliced;
            
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btn.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = EnsureIsSprite(iconAsset);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            btn.AddComponent<Button>();
        }

        private static Text AddHeaderText(GameObject win, string text, string iconAsset)
        {
            GameObject hBg = new GameObject("HeaderBG", typeof(RectTransform));
            hBg.transform.SetParent(win.transform, false);
            Image hImg = hBg.GetComponent<Image>() ?? hBg.AddComponent<Image>();
            hImg.raycastTarget = false; 
            Sprite sBg = EnsureIsSprite("shortBackgroundHeaderClass.png");
            if (sBg != null) {
                hImg.sprite = sBg;
                hImg.type = Image.Type.Simple;
            } else {
                hImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                hBg.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 26);
            }
            RectTransform hRect = hBg.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1); hRect.anchorMax = new Vector2(0.5f, 1);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = new Vector2(0, 0);
            hRect.sizeDelta = new Vector2(180, 26);
            
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(hBg.transform, false);
            Image icon = iconObj.GetComponent<Image>() ?? iconObj.AddComponent<Image>();
            icon.raycastTarget = false; 
            Sprite sIcon = EnsureIsSprite(iconAsset);
            if (sIcon != null) {
                icon.sprite = sIcon;
                icon.SetNativeSize();
            } else {
                icon.color = new Color(0, 0, 0, 0);
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
            }
            icon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(hBg.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.raycastTarget = false; 
            t.font = GetLegacyFont();
            t.text = text; t.fontSize = 12; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            Shadow s = tObj.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.5f);
            s.effectDistance = new Vector2(1, -1);

            return t;
        }
        private static void AddImageHeader(GameObject win, string headerAsset)
        {
            GameObject hBg = new GameObject("HeaderBG", typeof(RectTransform));
            hBg.transform.SetParent(win.transform, false);
            Image hImg = hBg.GetComponent<Image>() ?? hBg.AddComponent<Image>();
            hImg.raycastTarget = false; 
            Sprite sBg = EnsureIsSprite("shortBackgroundHeaderClass.png");
            if (sBg != null) {
                hImg.sprite = sBg;
                hImg.type = Image.Type.Simple;
            } else {
                hImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                hBg.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 26);
            }
            RectTransform hRect = hBg.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1); hRect.anchorMax = new Vector2(0.5f, 1);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = new Vector2(0, 0);
            hRect.sizeDelta = new Vector2(180, 26);
            
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(hBg.transform, false);
            Image icon = iconObj.GetComponent<Image>() ?? iconObj.AddComponent<Image>();
            icon.raycastTarget = false; 
            Sprite sIcon = EnsureIsSprite(headerAsset);
            if (sIcon != null) {
                icon.sprite = sIcon;
                icon.SetNativeSize();
            }
            icon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateChatMessagePrefab()
        {
            string path = "Assets/Prefabs/UI/ChatMessageItem.prefab";
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            
            GameObject go = new GameObject("ChatMessageItem", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 20);
            
            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childControlHeight = true; 
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 4;
            hlg.padding = new RectOffset(2, 2, 0, 0);

            // Added LayoutElement to tell VerticalLayoutGroup our preferred size
            LayoutElement mainLE = go.AddComponent<LayoutElement>();
            mainLE.minHeight = 16;

            GameObject rankObj = new GameObject("RankIcon", typeof(RectTransform));
            rankObj.transform.SetParent(go.transform, false);
            LayoutElement rankLE = rankObj.AddComponent<LayoutElement>();
            rankLE.minWidth = 16; rankLE.minHeight = 14;
            rankLE.preferredWidth = 16; rankLE.preferredHeight = 14;
            Image rankImg = rankObj.AddComponent<Image>();
            rankImg.preserveAspect = true;

            GameObject senderObj = new GameObject("Sender", typeof(RectTransform));
            senderObj.transform.SetParent(go.transform, false);
            Text senderText = senderObj.AddComponent<Text>();
            Font legacyFont = Resources.Load<Font>("LegacyRuntime") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            senderText.font = legacyFont != null ? legacyFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            senderText.fontSize = 12; senderText.fontStyle = FontStyle.Bold;
            senderText.color = Color.green; senderText.alignment = TextAnchor.MiddleLeft;
            senderObj.AddComponent<LayoutElement>().flexibleWidth = 0;
            senderObj.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Classic shadow effect
            Shadow sShadow = senderObj.AddComponent<Shadow>();
            sShadow.effectColor = new Color(0, 0, 0, 0.7f);
            sShadow.effectDistance = new Vector2(1, -1);

            GameObject msgObj = new GameObject("Message", typeof(RectTransform));
            msgObj.transform.SetParent(go.transform, false);
            Text msgText = msgObj.AddComponent<Text>();
            msgText.font = senderText.font;
            msgText.fontSize = 12; msgText.color = Color.white;
            msgText.alignment = TextAnchor.MiddleLeft;
            msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            msgText.verticalOverflow = VerticalWrapMode.Overflow;
            msgObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Text needs to drive the height
            msgObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Classic shadow effect
            Shadow mShadow = msgObj.AddComponent<Shadow>();
            mShadow.effectColor = new Color(0, 0, 0, 0.7f);
            mShadow.effectDistance = new Vector2(1, -1);

            ChatMessageItem item = go.AddComponent<ChatMessageItem>();
            SerializedObject so = new SerializedObject(item);
            so.FindProperty("_rankIcon").objectReferenceValue = rankImg;
            so.FindProperty("_senderText").objectReferenceValue = senderText;
            so.FindProperty("_messageText").objectReferenceValue = msgText;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePart(GameObject parent, string name, string asset, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Image.Type type, float rotation = 0)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos; rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0, 0, rotation);
            
            Image img = obj.AddComponent<Image>();
            Sprite s = EnsureIsSprite(asset);
            if (s != null)
            {
                img.sprite = s;
                img.type = type;
                img.raycastTarget = false; // Decorative by default
            }
            else
            {
                img.raycastTarget = false; // Decorative by default
                // Refined fallback look: Rounded glossy button simulation
                if (asset.Contains("ACTIVE") || name.Contains("Active"))
                    img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Neutral dark grey instead of green
                else if (asset.Contains("UP"))
                    img.color = new Color(0.25f, 0.25f, 0.25f, 1f); 
                else
                    img.color = new Color(0.05f, 0.05f, 0.05f, 0.4f); 
            }
            return obj;
        }



    }
}
