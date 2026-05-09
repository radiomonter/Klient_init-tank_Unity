using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Tanki.UI;
using Tanki.Networking;
using Tanki.Controllers;
using System.Reflection;

namespace Tanki.Editor
{
    public class EntranceUIBuilder : EditorWindow
    {
        private const string ASSET_PATH = "Assets/Textures/UI/images/";

        [MenuItem("Tanki/UI/Build Entrance UI")]
        public static void BuildUI()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Cannot build UI while in Play Mode!", "OK");
                return;
            }

            // Cleanup
            GameObject oldCanvas = GameObject.Find("EntranceCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);
            
            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                if (obj.transform.parent == null && (obj.name == "Remember" || obj.name == "Username" || obj.name == "Password"))
                {
                    if (obj.name != "EntranceCanvas" && obj.name != "NetworkManager" && obj.name != "Main Camera")
                        DestroyImmediate(obj);
                }
            }

            GameObject canvasObj = new GameObject("EntranceCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camObj.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = Color.black;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            CreateBackground(canvasObj);

            GameObject entranceUIObj = new GameObject("EntranceUI");
            entranceUIObj.transform.SetParent(canvasObj.transform, false);
            RectTransform entranceRect = entranceUIObj.AddComponent<RectTransform>();
            entranceRect.anchorMin = Vector2.zero;
            entranceRect.anchorMax = Vector2.one;
            entranceRect.offsetMin = Vector2.zero;
            entranceRect.offsetMax = Vector2.zero;

            entranceUIObj.AddComponent<CanvasGroup>().alpha = 1f;

            EntranceUIController controller = entranceUIObj.AddComponent<EntranceUIController>();
            controller.network = Object.FindObjectOfType<NetworkClient>();

            // 1. Login View
            GameObject loginView = new GameObject("LoginView");
            loginView.transform.SetParent(entranceUIObj.transform, false);
            RectTransform lvRect = loginView.AddComponent<RectTransform>();
            lvRect.sizeDelta = new Vector2(372, 300);
            lvRect.anchoredPosition = Vector2.zero;

            GameObject loginMain = CreateTankWindow(loginView, "MainWindow", 372, 193);
            loginMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            
            GameObject loginSocial = CreateTankWindow(loginView, "SocialWindow", 332, 85);
            loginSocial.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);

            AddHeaderText(loginMain, "1133_titanomachina.headers.Headers_loginRuHeaderClass.png");
            var lf = SetupLoginForm(loginMain, loginSocial);
            
            controller.loginView = loginView;
            controller.loginUsername = lf.user;
            controller.loginPassword = lf.pass;
            controller.rememberMe = lf.rem;
            controller.loginButton = lf.login;
            controller.forgotPasswordButton = lf.forgot;
            controller.toRegistrationButton = lf.toReg;

            // 2. Registration View
            GameObject regView = new GameObject("RegistrationView");
            regView.transform.SetParent(entranceUIObj.transform, false);
            RectTransform rvRect = regView.AddComponent<RectTransform>();
            rvRect.sizeDelta = new Vector2(380, 400);
            rvRect.anchoredPosition = Vector2.zero;

            GameObject regMain = CreateTankWindow(regView, "MainWindow", 380, 230);
            regMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            
            GameObject regSocial = CreateTankWindow(regView, "SocialWindow", 360, 85);
            regSocial.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -108);

            AddHeaderText(regMain, "1425_titanomachina.headers.Headers_registerRuHeaderClass.png");
            var rf = SetupRegistrationForm(regMain, regSocial);
            
            controller.registrationView = regView;
            controller.regUsername = rf.user;
            controller.regPassword = rf.pass;
            controller.regConfirmPassword = rf.conf;
            controller.regEmail = rf.email;
            controller.registerButton = rf.reg;
            controller.toLoginButton = rf.toLogin;

            // 3. Restore View
            GameObject restoreView = new GameObject("RestoreView");
            restoreView.transform.SetParent(entranceUIObj.transform, false);
            RectTransform rstvRect = restoreView.AddComponent<RectTransform>();
            rstvRect.sizeDelta = new Vector2(372, 300);
            rstvRect.anchoredPosition = Vector2.zero;

            GameObject restoreMain = CreateTankWindow(restoreView, "MainWindow", 372, 280);
            restoreMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

            AddHeaderText(restoreMain, "1287_titanomachina.headers.Headers_changePasswordRuHeaderClass.png");
            var rstf = SetupRestoreForm(restoreMain);

            controller.restoreView = restoreView;
            controller.restoreEmail = rstf.email;
            controller.recoverButton = rstf.recover;
            controller.cancelRestoreButton = rstf.cancel;
            
            regView.SetActive(false);
            restoreView.SetActive(false);
            entranceUIObj.SetActive(false);

            // Re-link to LobbyController
            LobbyController lobby = Object.FindObjectOfType<LobbyController>();
            if (lobby != null)
            {
                var field = typeof(LobbyController).GetField("_entranceUI", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(lobby, controller);
                    EditorUtility.SetDirty(lobby);
                    Debug.Log("[UI Builder] Re-linked to LobbyController.");
                }
            }

            Selection.activeGameObject = entranceUIObj;
            Debug.Log("[UI Builder] UI rebuilt.");
        }

        private static void CreateBackground(GameObject parent)
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(parent.transform, false);
            Image img = bg.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "1029_controls.dropdownlist.AccountsBackground_bitmapBG.jpg");
            img.type = Image.Type.Tiled;
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform rect = bg.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; 
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTankWindow(GameObject parent, string name, int w, int h)
        {
            GameObject win = new GameObject(name);
            win.transform.SetParent(parent.transform, false);
            RectTransform rect = win.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);

            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(win.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "WindowBGTile.jpg");
            bgImg.type = Image.Type.Tiled;
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(7, 7); bgRect.offsetMax = new Vector2(-7, -7);

            float f = 11;
            // Frames
            CreatePart(win, "Top", ASSET_PATH + "WindowTop.png", new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -f/2), new Vector2(w - 22, f), Image.Type.Tiled);
            CreatePart(win, "Bottom", ASSET_PATH + "WindowBottom.png", new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, f/2), new Vector2(w - 22, f), Image.Type.Tiled);
            CreatePart(win, "Left", ASSET_PATH + "WindowLeft.png", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(f/2, 0), new Vector2(f, h - 22), Image.Type.Tiled);
            CreatePart(win, "Right", ASSET_PATH + "WindowRight.png", new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-f/2, 0), new Vector2(f, h - 22), Image.Type.Tiled);

            // Corners - Using new named assets
            CreatePart(win, "TL", ASSET_PATH + "WindowTopLeftCorner.png", new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "TR", ASSET_PATH + "WindowTopRightCorner.png", new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BR", ASSET_PATH + "WindowBottomRightCorner.png", new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-f/2, f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(win, "BL", ASSET_PATH + "WindowBottomLeftCorner.png", new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(f/2, f/2), new Vector2(f, f), Image.Type.Simple, 0);

            return win;
        }

        private static void AddHeaderText(GameObject win, string textAsset)
        {
            GameObject hBg = new GameObject("HeaderBG");
            hBg.transform.SetParent(win.transform, false);
            Image hImg = hBg.AddComponent<Image>();
            hImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "957_resources.windowheaders.background.BackgroundHeader_shortBackgroundHeaderClass.png");
            hImg.type = Image.Type.Sliced;
            RectTransform hRect = hBg.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1); hRect.anchorMax = new Vector2(0.5f, 1);
            hRect.anchoredPosition = new Vector2(0, 0); 
            hRect.sizeDelta = new Vector2(200, 30);

            GameObject hTxt = new GameObject("Text");
            hTxt.transform.SetParent(hBg.transform, false);
            Image tImg = hTxt.AddComponent<Image>();
            tImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + textAsset);
            tImg.SetNativeSize();
        }

        private struct LoginFields { public InputField user, pass; public Toggle rem; public Button login, toReg, forgot; }
        private static LoginFields SetupLoginForm(GameObject main, GameObject social)
        {
            LoginFields f = new LoginFields();
            f.toReg = CreateLinkLabel(main, "NowPlayer", new Vector2(-60, 75), "Новый игрок", Color.green);
            f.forgot = CreateLinkLabel(main, "Forgot", new Vector2(60, 75), "Забыли имя или пароль?", Color.green);

            CreateLabel(main, "UserLabel", new Vector2(-70, 45), "Имя или email:");
            f.user = CreateAuthenticInput(main, "Username", new Vector2(50, 45), 200);
            
            CreateLabel(main, "PassLabel", new Vector2(-70, 15), "Пароль:");
            f.pass = CreateAuthenticInput(main, "Password", new Vector2(50, 15), 200, true);

            f.rem = CreateAuthenticToggle(main, "Remember", new Vector2(-40, -25), "Запомнить");
            f.login = CreateAuthenticButton(main, "Play", new Vector2(110, -25), "Играть");

            SetupSocialBlock(social, "Войти в игру через сервис");

            return f;
        }

        private struct RegFields { public InputField user, pass, conf, email; public Button reg, toLogin; }
        private static RegFields SetupRegistrationForm(GameObject main, GameObject social)
        {
            RegFields f = new RegFields();
            f.toLogin = CreateLinkLabel(main, "ToLogin", new Vector2(-60, 95), "Я уже зарегистрирован", Color.green);

            CreateLabel(main, "UserLabel", new Vector2(-70, 65), "Имя или email:");
            f.user = CreateAuthenticInput(main, "Username", new Vector2(50, 65), 200);
            
            CreateLabel(main, "PassLabel", new Vector2(-70, 38), "Пароль:");
            f.pass = CreateAuthenticInput(main, "Password", new Vector2(50, 38), 200, true);

            CreateLabel(main, "ConfLabel", new Vector2(-70, 11), "Повтор:");
            f.conf = CreateAuthenticInput(main, "Confirm", new Vector2(50, 11), 200, true);

            CreateLabel(main, "EmailLabel", new Vector2(-70, -16), "E-mail:");
            f.email = CreateAuthenticInput(main, "Email", new Vector2(50, -16), 200);

            CreateAuthenticToggle(main, "Remember", new Vector2(-40, -55), "Запомнить");
            f.reg = CreateAuthenticButton(main, "Register", new Vector2(110, -55), "Играть");

            SetupSocialBlock(social, "Зарегистрироваться через сервис");

            return f; 
        }

        private struct RestoreFields { public InputField email; public Button recover, cancel; }
        private static RestoreFields SetupRestoreForm(GameObject main)
        {
            RestoreFields f = new RestoreFields();
            
            CreateLabel(main, "HelpLabel", new Vector2(0, 70), "Введите ваш e-mail. Вам будет отправлено\nписьмо со ссылкой для смены пароля.", TextAnchor.MiddleCenter);

            CreateLabel(main, "EmailLabel", new Vector2(-70, 20), "E-mail:");
            f.email = CreateAuthenticInput(main, "Email", new Vector2(50, 20), 200);

            // Placeholder for Captcha
            CreateLabel(main, "CaptchaLabel", new Vector2(0, -30), "[ СЕКЦИЯ КАПЧИ ]", TextAnchor.MiddleCenter);

            f.recover = CreateAuthenticButton(main, "Recover", new Vector2(-60, -85), "Восстановить");
            f.cancel = CreateAuthenticButton(main, "Cancel", new Vector2(60, -85), "Отмена");

            return f;
        }

        private static void SetupSocialBlock(GameObject social, string label)
        {
            CreateLabel(social, "SocialLabel", new Vector2(0, 15), label, TextAnchor.MiddleCenter);
            GameObject vk = new GameObject("VKButton");
            vk.transform.SetParent(social.transform, false);
            Image vkImg = vk.AddComponent<Image>();
            vkImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "980_controls.VKButton_releaseBitmapVK.png");
            vkImg.SetNativeSize();
            vk.AddComponent<Button>();
            vk.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);
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
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
            img.type = type;
            return obj;
        }

        private static InputField CreateAuthenticInput(GameObject parent, string name, Vector2 pos, float width, bool isPass = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(width, 26);
            
            CreatePart(obj, "L", ASSET_PATH + "102_assets.input.InputLeft_assets.input.InputLeft.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(10, 26), Image.Type.Simple);
            CreatePart(obj, "R", ASSET_PATH + "101_assets.input.InputRight_assets.input.InputRight.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(10, 26), Image.Type.Simple);
            CreatePart(obj, "M", ASSET_PATH + "106_assets.input.InputCenter_assets.input.InputCenter.png", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 20, 26), Image.Type.Tiled);
            
            InputField input = obj.AddComponent<InputField>();
            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(obj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white; t.alignment = TextAnchor.MiddleLeft; t.fontSize = 12;
            RectTransform tRect = t.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(10, 2); tRect.offsetMax = new Vector2(-10, -2);
            input.textComponent = t;
            if (isPass) input.contentType = InputField.ContentType.Password;
            return input;
        }

        private static Button CreateAuthenticButton(GameObject parent, string name, Vector2 pos, string label)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(100, 30);
            Button btn = obj.AddComponent<Button>();
            
            CreatePart(obj, "L", ASSET_PATH + "1117_controls.buttons.h50px.GreyBigButtonSkin_leftUpClass.png", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(10, 30), Image.Type.Simple);
            CreatePart(obj, "R", ASSET_PATH + "879_controls.buttons.h50px.GreyBigButtonSkin_rightUpClass.png", new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(10, 30), Image.Type.Simple);
            CreatePart(obj, "M", ASSET_PATH + "1072_controls.buttons.h50px.GreyBigButtonSkin_middleUpClass.png", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 30), Image.Type.Tiled);
            
            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(obj.transform, false);
            Text t = tObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white; t.text = label; t.alignment = TextAnchor.MiddleCenter; t.fontSize = 13;
            t.GetComponent<RectTransform>().anchorMin = Vector2.zero; t.GetComponent<RectTransform>().anchorMax = Vector2.one;
            return btn;
        }

        private static void CreateLabel(GameObject parent, string name, Vector2 pos, string text, TextAnchor align = TextAnchor.MiddleRight)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white; t.text = text; t.alignment = align; t.fontSize = 13;
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (align == TextAnchor.MiddleCenter) rect.pivot = new Vector2(0.5f, 0.5f);
            else rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(300, 20);
        }

        private static Button CreateLinkLabel(GameObject parent, string name, Vector2 pos, string text, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = color; t.text = text; t.alignment = TextAnchor.MiddleCenter; t.fontSize = 12; t.fontStyle = FontStyle.Bold;
            obj.AddComponent<Button>();
            obj.GetComponent<RectTransform>().anchoredPosition = pos; obj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 20);
            return obj.GetComponent<Button>();
        }

        private static Toggle CreateAuthenticToggle(GameObject parent, string name, Vector2 pos, string label)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(120, 20);
            Toggle toggle = obj.AddComponent<Toggle>();
            
            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(obj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1);
            bg.GetComponent<RectTransform>().anchoredPosition = new Vector2(-45, 0); 
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
            
            toggle.targetGraphic = bgImg; 
            
            GameObject check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform, false);
            Image cImg = check.AddComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ASSET_PATH + "107_assets.input.InputCheck_assets.input.InputCheck.png");
            if (cImg.sprite == null) cImg.color = new Color(1, 1, 1, 0.8f);
            check.GetComponent<RectTransform>().sizeDelta = new Vector2(12, 12);
            toggle.graphic = cImg;
            
            GameObject l = new GameObject("Label");
            l.transform.SetParent(obj.transform, false);
            Text t = l.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white; t.text = label; t.fontSize = 12;
            l.GetComponent<RectTransform>().anchoredPosition = new Vector2(15, 0); 
            l.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 20);
            return toggle;
        }
    }
}
