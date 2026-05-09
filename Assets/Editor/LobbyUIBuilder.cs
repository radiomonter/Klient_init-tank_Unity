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

namespace Tanki.Editor
{
    public class LobbyUIBuilder : EditorWindow
    {
        private const string ASSET_PATH = "Assets/Textures/UI/images/";

        private static Sprite EnsureIsSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string path = name.StartsWith("Assets") ? name : ASSET_PATH + name;
            
            // Try to find the asset if it doesn't exist at exact path (handle ID prefixes)
            if (!File.Exists(path))
            {
                string fileName = Path.GetFileName(name);
                string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.Contains(ASSET_PATH))
                    {
                        path = p;
                        break;
                    }
                }
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (importer.textureType != TextureImporterType.Sprite || settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
            
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/images/740_alternativa.tanks.bg.BackgroundService_bitmapBg.png");
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
            
            gRect.anchorMin = Vector2.zero; gRect.anchorMax = Vector2.one;
            gRect.offsetMin = Vector2.zero; gRect.offsetMax = Vector2.zero;

            GameObject topPanel = BuildTopPanel(lobbyUIObj);
            
            GameObject mainContent = new GameObject("MainContent");
            mainContent.transform.SetParent(lobbyUIObj.transform, false);
            RectTransform mainRect = mainContent.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero; mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = new Vector2(5, 5);
            mainRect.offsetMax = new Vector2(-5, -60);

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
            AddHeaderText(battleListPanel, "СПИСОК БИТВ", "HeaderBattleList.png");

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
            AddHeaderText(battleInfoPanel, "ИНФОРМАЦИЯ О БИТВЕ", "HeaderBattleInfo.png");

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
            AddHeaderText(garageWin, "МОЙ ТАНК", "HeaderGarage.png");
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
            AddHeaderText(communicationPanel, "ЧАТ", "HeaderChat.png");
            SetupBattleList(battleListPanel);
            NewsController newsCtrl = col1.GetComponentInChildren<NewsController>();

            LobbyController lobbyCtrl = Object.FindObjectOfType<LobbyController>();
            if (lobbyCtrl != null)
            {
                SerializedObject soLobby = new SerializedObject(lobbyCtrl);
                soLobby.FindProperty("_lobbyUI").objectReferenceValue = controller;
                soLobby.FindProperty("_chatUI").objectReferenceValue = chatCtrl;
                
                newsCtrl = communicationPanel.GetComponent<NewsController>();
                soLobby.FindProperty("_newsUI").objectReferenceValue = newsCtrl;
                
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
                EditorUtility.SetDirty(lobbyCtrl);
            }

            if (!isPlaying)
            {
                EditorUtility.SetDirty(controller);
                if (lobbyCtrl != null) EditorUtility.SetDirty(lobbyCtrl);
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
            GameObject top = new GameObject("TopPanel");
            top.transform.SetParent(parent.transform, false);
            RectTransform rect = top.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, -30); rect.sizeDelta = new Vector2(0, 60);

            Image bg = top.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);

            GameObject playerSection = new GameObject("PlayerSection");
            playerSection.AddComponent<RectTransform>();
            playerSection.transform.SetParent(top.transform, false);
            RectTransform psRect = playerSection.GetComponent<RectTransform>();
            psRect.anchorMin = new Vector2(0, 0); psRect.anchorMax = new Vector2(0.5f, 1);
            psRect.offsetMin = new Vector2(15, 0); psRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = playerSection.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft; hlg.spacing = 15;
            hlg.childControlWidth = false; hlg.childForceExpandWidth = false;

            GameObject rank = new GameObject("Rank", typeof(RectTransform));
            rank.transform.SetParent(playerSection.transform, false);
            rank.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
            Image rankImg = rank.AddComponent<Image>();
            Sprite rankSprite = EnsureIsSprite("Rank10.png");
            if (rankSprite != null) rankImg.sprite = rankSprite;
            else rankImg.color = new Color(1, 1, 1, 0.1f);

            GameObject nameExp = new GameObject("NameExp", typeof(RectTransform));
            nameExp.transform.SetParent(playerSection.transform, false);
            nameExp.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40);
            VerticalLayoutGroup vlg = nameExp.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft; vlg.spacing = 2;

            Text nameTxt = new GameObject("Name", typeof(Text)).GetComponent<Text>();
            nameTxt.transform.SetParent(nameExp.transform, false);
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.text = "PlayerName"; nameTxt.fontSize = 14; nameTxt.color = new Color(0.7f, 1f, 0.3f);

            GameObject expBarObj = new GameObject("ExpBar", typeof(RectTransform));
            expBarObj.transform.SetParent(nameExp.transform, false);
            expBarObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 10);
            Slider expSlider = expBarObj.AddComponent<Slider>();
            expBarObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(expBarObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
            faRect.offsetMin = new Vector2(1, 1); faRect.offsetMax = new Vector2(-1, -1);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.8f, 0.1f, 1f);
            expSlider.fillRect = fill.GetComponent<RectTransform>();
            expSlider.minValue = 0; expSlider.maxValue = 100; expSlider.value = 45;

            GameObject cryGroup = new GameObject("Crystals", typeof(RectTransform));
            cryGroup.transform.SetParent(playerSection.transform, false);
            cryGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            HorizontalLayoutGroup cryHlg = cryGroup.AddComponent<HorizontalLayoutGroup>();
            cryHlg.childAlignment = TextAnchor.MiddleLeft; cryHlg.spacing = 5;

            GameObject ci = new GameObject("Icon", typeof(RectTransform));
            ci.transform.SetParent(cryGroup.transform, false);
            ci.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 18);
            Image cryImg = ci.AddComponent<Image>();
            Sprite crySprite = EnsureIsSprite("CrystalIcon.png");
            if (crySprite != null) cryImg.sprite = crySprite;
            else cryImg.color = new Color(0, 0.8f, 1f, 0.5f);

            Text cryTxt = new GameObject("Amount", typeof(Text)).GetComponent<Text>();
            cryTxt.transform.SetParent(cryGroup.transform, false);
            cryTxt.font = nameTxt.font; cryTxt.text = "0"; cryTxt.fontSize = 14; cryTxt.color = Color.white;

            GameObject scoreGroup = new GameObject("Score", typeof(RectTransform));
            scoreGroup.transform.SetParent(playerSection.transform, false);
            scoreGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
            Text scoreTxt = new GameObject("Value", typeof(Text)).GetComponent<Text>();
            scoreTxt.transform.SetParent(scoreGroup.transform, false);
            scoreTxt.font = nameTxt.font; scoreTxt.text = "0"; scoreTxt.fontSize = 14; scoreTxt.color = new Color(0.9f, 0.9f, 0.1f);
            scoreTxt.GetComponent<RectTransform>().anchorMin = Vector2.zero; scoreTxt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            scoreTxt.alignment = TextAnchor.MiddleLeft;

            GameObject menu = new GameObject("Menu", typeof(RectTransform));
            menu.transform.SetParent(top.transform, false);
            RectTransform mRect = menu.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0); mRect.anchorMax = new Vector2(1, 1);
            mRect.offsetMin = new Vector2(0, 0); mRect.offsetMax = new Vector2(-20, 0);

            CreateMenuButton(menu, "Battles", new Vector2(-210, 0), "БИТВЫ");
            CreateMenuButton(menu, "Garage", new Vector2(-110, 0), "ГАРАЖ");
            CreateMenuButton(menu, "Settings", new Vector2(-10, 0), "НАСТРОЙКИ");

            TopPanelController ctrl = top.AddComponent<TopPanelController>();
            
            string varPath = "Assets/Data/Variables";
            SerializedObject soTop = new SerializedObject(ctrl);
            soTop.FindProperty("_uid").objectReferenceValue = EnsureAsset<StringVariable>(varPath + "/Uid.asset");
            soTop.FindProperty("_rank").objectReferenceValue = EnsureAsset<IntVariable>(varPath + "/Rank.asset");
            soTop.FindProperty("_crystals").objectReferenceValue = EnsureAsset<IntVariable>(varPath + "/Crystals.asset");
            soTop.FindProperty("_score").objectReferenceValue = EnsureAsset<IntVariable>(varPath + "/Score.asset");
            soTop.FindProperty("_nextRankScore").objectReferenceValue = EnsureAsset<IntVariable>(varPath + "/NextRankScore.asset");
            
            soTop.FindProperty("_uidText").objectReferenceValue = nameTxt;
            soTop.FindProperty("_rankIcon").objectReferenceValue = rankImg;
            soTop.FindProperty("_crystalsText").objectReferenceValue = cryTxt;
            soTop.FindProperty("_scoreText").objectReferenceValue = scoreTxt;
            soTop.FindProperty("_rankProgress").objectReferenceValue = expSlider;

            string[] guids = AssetDatabase.FindAssets("bitmapBigRank t:Sprite");
            if (guids.Length > 0)
            {
                var spritesList = new System.Collections.Generic.List<Sprite>();
                for(int i=0; i<40; i++) spritesList.Add(null);

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null)
                    {
                        string n = s.name;
                        int lastRankIdx = n.LastIndexOf("Rank");
                        if (lastRankIdx != -1)
                        {
                            string numStr = "";
                            for(int i = lastRankIdx + 4; i < n.Length; i++)
                            {
                                if (char.IsDigit(n[i])) numStr += n[i];
                                else break;
                            }
                            if (int.TryParse(numStr, out int rankIdx) && rankIdx < spritesList.Count)
                                spritesList[rankIdx] = s;
                        }
                    }
                }
                
                SerializedProperty rankArray = soTop.FindProperty("_rankSprites");
                rankArray.arraySize = spritesList.Count;
                for (int i = 0; i < spritesList.Count; i++)
                    rankArray.GetArrayElementAtIndex(i).objectReferenceValue = spritesList[i];
            }

            soTop.ApplyModifiedProperties();

            return top;
        }

        private static ChatController SetupCommunicationPanel(GameObject panel)
        {
            // Фон "серой полки" - отдельный объект, смещенный вправо.
            GameObject shelfObj = new GameObject("ShelfBG");
            shelfObj.transform.SetParent(panel.transform, false);
            RectTransform shelfRect = shelfObj.AddComponent<RectTransform>();
            shelfRect.anchorMin = new Vector2(0, 1); shelfRect.anchorMax = new Vector2(1, 1);
            shelfRect.pivot = new Vector2(0.5f, 1);
            shelfRect.anchoredPosition = new Vector2(0, -1); 
            shelfRect.sizeDelta = new Vector2(0, 25); 
            // Сдвиньте полку вправо так, чтобы она начиналась после кнопок.
            shelfRect.offsetMin = new Vector2(220, 0); 
            shelfRect.offsetMax = new Vector2(0, 0);

            Image shelfImg = shelfObj.AddComponent<Image>();
            Sprite shelfSprite = EnsureIsSprite("957_resources.windowheaders.background.BackgroundHeader_shortBackgroundHeaderClass.png");
            if (shelfSprite == null) shelfSprite = EnsureIsSprite("HeaderBackground.png");
            
            if (shelfSprite != null) {
                shelfImg.sprite = shelfSprite;
                shelfImg.type = Image.Type.Simple;
            } else {
                shelfImg.color = new Color(0.15f, 0.15f, 0.15f, 1f); 
            }
            
            // Кнопки размещаются непосредственно дочерними элементами панели, чтобы избежать проблем со смещением родительского элемента.
            GameObject newsTab = CreateCommunicationTab(panel, "NewsTab", "Новости", "1034_alternativa.tanks.gui.communication.button.TabIcons_newsIconClass.png", true);
            GameObject chatTab = CreateCommunicationTab(panel, "ChatTab", "Чат", "770_alternativa.tanks.gui.communication.button.TabIcons_chatIconClass.png", false);
            
            newsTab.GetComponent<RectTransform>().anchoredPosition = new Vector2(11, -11);
            chatTab.GetComponent<RectTransform>().anchoredPosition = new Vector2(118, -11); // 11 + 102 + 5

            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(panel.transform, false);
            RectTransform caRect = contentArea.AddComponent<RectTransform>();
            caRect.anchorMin = Vector2.zero; caRect.anchorMax = Vector2.one;
            caRect.offsetMin = new Vector2(5, 5); caRect.offsetMax = new Vector2(-5, -45); 

            GameObject newsView = new GameObject("NewsView");
            newsView.transform.SetParent(contentArea.transform, false);
            RectTransform nvRect = newsView.AddComponent<RectTransform>();
            nvRect.anchorMin = Vector2.zero; nvRect.anchorMax = Vector2.one;
            nvRect.offsetMin = Vector2.zero; nvRect.offsetMax = Vector2.zero;
            
            GameObject scrollObj = new GameObject("NewsScroll", typeof(RectTransform));
            scrollObj.transform.SetParent(newsView.transform, false);
            RectTransform sRect = scrollObj.GetComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero; sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(5, 5); sRect.offsetMax = new Vector2(-5, -5);
            scrollObj.AddComponent<Mask>();
            scrollObj.AddComponent<Image>().color = new Color(0,0,0,0.01f);

            GameObject newsContent = new GameObject("Content", typeof(RectTransform));
            newsContent.transform.SetParent(scrollObj.transform, false);
            RectTransform ncRect = newsContent.GetComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0, 1); ncRect.anchorMax = new Vector2(1, 1);
            ncRect.pivot = new Vector2(0.5f, 1);
            ncRect.sizeDelta = new Vector2(0, 0); // Заполнить ширину родительского элемента
            VerticalLayoutGroup vlg = newsContent.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.spacing = 15; 
            vlg.padding = new RectOffset(5, 5, 10, 10);
            newsContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect srNews = scrollObj.AddComponent<ScrollRect>();
            srNews.content = ncRect; srNews.vertical = true; srNews.horizontal = false;

            GameObject chatView = new GameObject("ChatView", typeof(RectTransform));
            chatView.transform.SetParent(contentArea.transform, false);
            RectTransform cvRect = chatView.GetComponent<RectTransform>();
            cvRect.anchorMin = Vector2.zero; cvRect.anchorMax = Vector2.one;

            GameObject chatScrollObj = new GameObject("ChatScroll", typeof(RectTransform));
            chatScrollObj.transform.SetParent(chatView.transform, false);
            RectTransform csRect = chatScrollObj.GetComponent<RectTransform>();
            csRect.anchorMin = Vector2.zero; csRect.anchorMax = Vector2.one;
            csRect.offsetMin = new Vector2(0, 35);
            chatScrollObj.AddComponent<Mask>();
            chatScrollObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);

            GameObject chatContent = new GameObject("Content", typeof(RectTransform));
            chatContent.transform.SetParent(chatScrollObj.transform, false);
            Text chatDisplayText = chatContent.AddComponent<Text>();
            chatDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            chatDisplayText.fontSize = 12; chatDisplayText.color = Color.white;
            chatDisplayText.alignment = TextAnchor.LowerLeft;
            chatDisplayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            chatDisplayText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform ccRect = chatContent.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0, 0); ccRect.anchorMax = new Vector2(1, 0);
            ccRect.pivot = new Vector2(0.5f, 0); ccRect.sizeDelta = new Vector2(0, 300);

            ScrollRect srChat = chatScrollObj.AddComponent<ScrollRect>();
            srChat.content = ccRect; srChat.vertical = true; srChat.horizontal = false;

            GameObject inputObj = new GameObject("InputField", typeof(RectTransform));
            inputObj.transform.SetParent(chatView.transform, false);
            RectTransform iRect = inputObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 0); iRect.anchorMax = new Vector2(1, 0);
            iRect.anchoredPosition = new Vector2(-25, 15); iRect.sizeDelta = new Vector2(-60, 24);
            
            InputField input = inputObj.AddComponent<InputField>();
            inputObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            GameObject tObj = new GameObject("Text", typeof(RectTransform));
            tObj.transform.SetParent(inputObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = chatDisplayText.font; t.color = Color.white; t.fontSize = 12; t.alignment = TextAnchor.MiddleLeft;
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;
            t.GetComponent<RectTransform>().offsetMin = new Vector2(5, 0);
            input.textComponent = t;

            GameObject sendBtn = new GameObject("Send", typeof(RectTransform));
            sendBtn.transform.SetParent(chatView.transform, false);
            RectTransform sbRect = sendBtn.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0); sbRect.anchorMax = new Vector2(1, 0);
            sbRect.anchoredPosition = new Vector2(-15, 15); sbRect.sizeDelta = new Vector2(30, 24);
            Button btn = sendBtn.AddComponent<Button>();
            sendBtn.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

            chatView.SetActive(false);
            newsView.SetActive(true);

            ChatController chatCtrl = panel.AddComponent<ChatController>();
            typeof(ChatController).GetField("_scrollRect", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chatCtrl, srChat);
            typeof(ChatController).GetField("_chatDisplayText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chatCtrl, chatDisplayText);
            typeof(ChatController).GetField("_chatInputField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chatCtrl, input);
            typeof(ChatController).GetField("_sendButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chatCtrl, btn);

            NewsController newsCtrl = panel.AddComponent<NewsController>();
            SerializedObject soNews = new SerializedObject(newsCtrl);
            soNews.FindProperty("_container").objectReferenceValue = newsContent.transform;
            soNews.FindProperty("_frameSprite").objectReferenceValue = EnsureIsSprite("GreenFrameSkin_frame.png");
            soNews.FindProperty("_cornerSprite").objectReferenceValue = EnsureIsSprite("GreenFrameSkin_corner_frame.png");
            soNews.ApplyModifiedProperties();

            newsTab.GetComponent<Button>().onClick.AddListener(() => {
                newsView.SetActive(true);
                chatView.SetActive(false);
            });
            chatTab.GetComponent<Button>().onClick.AddListener(() => {
                newsView.SetActive(false);
                chatView.SetActive(true);
            });

            return chatCtrl;
        }

        private static GameObject CreateCommunicationTab(GameObject parent, string name, string label, string iconAsset, bool isActive)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(102, 30); // Ширина 102, согласно LobbyChat.as
            
            Button btn = btnObj.AddComponent<Button>();
            
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(btnObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

            string leftAsset, centerAsset, rightAsset;
            if (isActive) {
                // Активная вкладка ЗЕЛЕНАЯ (состояние закрытой кнопки GreenMediumButtonSkin)
                leftAsset = "802_controls.buttons.h30px.GreenMediumButtonSkin_leftDownClass.png";
                centerAsset = "1178_controls.buttons.h30px.GreenMediumButtonSkin_middleDownClass.png";
                rightAsset = "951_controls.buttons.h30px.GreenMediumButtonSkin_rightDownClass.png";
            } else {
                // Неактивная вкладка серая (активное состояние кнопки button_def).
                leftAsset = "27_assets.button.button_def_UP_LEFT.png";
                centerAsset = "21_assets.button.button_def_UP_CENTER.png";
                rightAsset = "14_assets.button.button_def_UP_RIGHT.png";
            }

            // Усовершенствованная трехкомпонентная система крепления для предотвращения зазоров.
            CreatePart(bg, "Left", leftAsset, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Right", rightAsset, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(8, 30), Image.Type.Simple);
            CreatePart(bg, "Center", centerAsset, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, Image.Type.Sliced);
            
            // Adjust Center offsets to fit between left/right parts
            RectTransform centerRect = bg.transform.Find("Center").GetComponent<RectTransform>();
            centerRect.anchorMin = Vector2.zero; centerRect.anchorMax = Vector2.one;
            centerRect.offsetMin = new Vector2(8, 0); centerRect.offsetMax = new Vector2(-8, 0);

            // Add Icon
            GameObject icoObj = new GameObject("Icon");
            icoObj.transform.SetParent(btnObj.transform, false);
            RectTransform icoRect = icoObj.AddComponent<RectTransform>();
            icoRect.anchorMin = new Vector2(0, 0.5f); icoRect.anchorMax = new Vector2(0, 0.5f);
            icoRect.anchoredPosition = new Vector2(12, 0); 
            Image icoImg = icoObj.AddComponent<Image>();
            Sprite sIcon = EnsureIsSprite(iconAsset);
            if (sIcon != null) {
                icoImg.sprite = sIcon;
                icoImg.SetNativeSize();
            } else {
                icoImg.color = new Color(1, 1, 1, 0.2f);
                icoRect.sizeDelta = new Vector2(16, 16);
            }

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.Load<Font>("LegacyRuntime") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            t.text = label.ToUpper(); // Flash often uses upper case for these tabs
            t.fontSize = 11; t.alignment = TextAnchor.MiddleLeft; 
            t.color = isActive ? Color.white : new Color(0.85f, 0.85f, 0.85f);
            t.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
            
            RectTransform tRect = tObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0); tRect.anchorMax = new Vector2(1, 1);
            tRect.offsetMin = new Vector2(30, 0); tRect.offsetMax = new Vector2(-5, 0);
            
            t.transform.SetAsLastSibling();
            return btnObj;
        }

        private static void SetupBattleList(GameObject panel)
        {
            GameObject findBtnObj = new GameObject("FindBattle");
            findBtnObj.transform.SetParent(panel.transform, false);
            RectTransform fbRect = findBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 1); fbRect.anchorMax = new Vector2(0.5f, 1);
            fbRect.anchoredPosition = new Vector2(0, -12); fbRect.sizeDelta = new Vector2(200, 24);
            Button findBtn = findBtnObj.AddComponent<Button>();
            Image findImg = findBtnObj.AddComponent<Image>();
            findImg.sprite = EnsureIsSprite("ButtonGo.png");
            if (findImg.sprite == null) findImg.color = new Color(0.2f, 0.6f, 0.2f);
            findImg.type = Image.Type.Sliced;

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
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.content = cRect; sr.vertical = true; sr.horizontal = false;

            typeof(BattleListController).GetField("_container", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ctrl, content.transform);
        }

        private static void CreateMenuButton(GameObject parent, string name, Vector2 pos, string label)
        {
            GameObject btnObj = new GameObject(name + "Button");
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(100, 30);
            
            Button btn = btnObj.AddComponent<Button>();
            Image img = btnObj.AddComponent<Image>();
            img.sprite = EnsureIsSprite("ButtonDefault.png");
            img.type = Image.Type.Sliced;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(btnObj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label; t.fontSize = 12; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;

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
            Sprite bgSprite = EnsureIsSprite("WindowBGTile.png");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Tiled;
            }
            else bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(7, 7); bgRect.offsetMax = new Vector2(-7, -7);

            float f = 11;
            CreatePart(win, "Top", "WindowTop.png", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -f/2), new Vector2(w > 0 ? w - 22 : 0, f), Image.Type.Tiled);
            CreatePart(win, "Bottom", "WindowBottom.png", new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, f/2), new Vector2(w > 0 ? w - 22 : 0, f), Image.Type.Tiled);
            CreatePart(win, "Left", "WindowLeft.png", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(f/2, 0), new Vector2(f, h > 0 ? h - 22 : 0), Image.Type.Tiled);
            CreatePart(win, "Right", "WindowRight.png", new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-f/2, 0), new Vector2(f, h > 0 ? h - 22 : 0), Image.Type.Tiled);

            if (w == 0 || h == 0)
            {
                win.transform.Find("Top").GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                win.transform.Find("Top").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                win.transform.Find("Top").GetComponent<RectTransform>().offsetMin = new Vector2(11, -11);
                win.transform.Find("Top").GetComponent<RectTransform>().offsetMax = new Vector2(-11, 0);

                win.transform.Find("Bottom").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().offsetMin = new Vector2(11, 0);
                win.transform.Find("Bottom").GetComponent<RectTransform>().offsetMax = new Vector2(-11, 11);

                win.transform.Find("Left").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                win.transform.Find("Left").GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                win.transform.Find("Left").GetComponent<RectTransform>().offsetMin = new Vector2(0, 11);
                win.transform.Find("Left").GetComponent<RectTransform>().offsetMax = new Vector2(11, -11);

                win.transform.Find("Right").GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                win.transform.Find("Right").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                win.transform.Find("Right").GetComponent<RectTransform>().offsetMin = new Vector2(-11, 11);
                win.transform.Find("Right").GetComponent<RectTransform>().offsetMax = new Vector2(0, -11);
            }

            CreatePart(win, "TL", "WindowTopLeftCorner.png", new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "TR", "WindowTopRightCorner.png", new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BR", "WindowBottomRightCorner.png", new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-f/2, f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BL", "WindowBottomLeftCorner.png", new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(f/2, f/2), new Vector2(f, f), Image.Type.Simple, 0);

            return win;
        }

        private static void AddHeaderText(GameObject win, string text, string iconAsset)
        {
            GameObject hBg = new GameObject("HeaderBG");
            hBg.transform.SetParent(win.transform, false);
            Image hImg = hBg.AddComponent<Image>();
            Sprite sBg = EnsureIsSprite("HeaderBackground.png");
            if (sBg != null) {
                hImg.sprite = sBg;
                hImg.type = Image.Type.Sliced;
            } else {
                hImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
            RectTransform hRect = hBg.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1); hRect.anchorMax = new Vector2(0.5f, 1);
            hRect.anchoredPosition = new Vector2(0, -2); 
            hRect.sizeDelta = new Vector2(180, 26);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(hBg.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            Sprite sIcon = EnsureIsSprite(iconAsset);
            if (sIcon != null) {
                icon.sprite = sIcon;
                icon.SetNativeSize();
            } else {
                icon.color = new Color(1, 1, 1, 0.2f);
            }
            icon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
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
            }
            else
            {
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

        private static GameObject CreateGreenFrame(GameObject parent)
        {
            GameObject frame = new GameObject("GreenFrame");
            frame.transform.SetParent(parent.transform, false);
            RectTransform rect = frame.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

            float f = 16; 
            CreatePart(frame, "TL", "GreenFrameSkin_corner_frame.png", new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(frame, "TR", "GreenFrameSkin_corner_frame.png", new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-f/2, -f/2), new Vector2(f, f), Image.Type.Simple, -90);
            CreatePart(frame, "BR", "GreenFrameSkin_corner_frame.png", new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-f/2, f/2), new Vector2(f, f), Image.Type.Simple, -180);
            CreatePart(frame, "BL", "GreenFrameSkin_corner_frame.png", new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(f/2, f/2), new Vector2(f, f), Image.Type.Simple, -270);

            CreatePart(frame, "Top", "GreenFrameSkin_frame.png", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -f/2), new Vector2(-f*2, f), Image.Type.Tiled);
            CreatePart(frame, "Bottom", "GreenFrameSkin_frame.png", new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, f/2), new Vector2(-f*2, f), Image.Type.Tiled, 180);
            CreatePart(frame, "Left", "GreenFrameSkin_frame.png", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(f/2, 0), new Vector2(f, -f*2), Image.Type.Tiled, 90);
            CreatePart(frame, "Right", "GreenFrameSkin_frame.png", new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-f/2, 0), new Vector2(f, -f*2), Image.Type.Tiled, -90);

            // Крепления для динамического изменения размера
            frame.transform.Find("Top").GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            frame.transform.Find("Top").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            frame.transform.Find("Top").GetComponent<RectTransform>().offsetMin = new Vector2(f, -f);
            frame.transform.Find("Top").GetComponent<RectTransform>().offsetMax = new Vector2(-f, 0);

            frame.transform.Find("Bottom").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            frame.transform.Find("Bottom").GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            frame.transform.Find("Bottom").GetComponent<RectTransform>().offsetMin = new Vector2(f, 0);
            frame.transform.Find("Bottom").GetComponent<RectTransform>().offsetMax = new Vector2(-f, f);

            frame.transform.Find("Left").GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            frame.transform.Find("Left").GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            frame.transform.Find("Left").GetComponent<RectTransform>().offsetMin = new Vector2(0, f);
            frame.transform.Find("Left").GetComponent<RectTransform>().offsetMax = new Vector2(f, -f);

            frame.transform.Find("Right").GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
            frame.transform.Find("Right").GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            frame.transform.Find("Right").GetComponent<RectTransform>().offsetMin = new Vector2(-f, f);
            frame.transform.Find("Right").GetComponent<RectTransform>().offsetMax = new Vector2(0, -f);

            return frame;
        }
    }
}
