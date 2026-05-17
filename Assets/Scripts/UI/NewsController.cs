using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    public class NewsController : MonoBehaviour
    {
        [SerializeField] private Models.UserDataSO _userData;
        [SerializeField] private Transform _container;
        [SerializeField] private Sprite _frameSprite;
        [SerializeField] private Sprite _cornerSprite;
        [SerializeField] private Sprite _bgTile;
        
        [System.Serializable]
        public class NewsList { public NewsData[] news; }

        [System.Serializable]
        public class NewsData
        {
            public string image;
            public string date;
            public string header;
            public string id;
            public string text;
        }

        public void SetNewsJson(string json)
        {
            try
            {
                if (_container == null) return;
                foreach (Transform child in _container) { if (child != null) Destroy(child.gameObject); }
                if (string.IsNullOrEmpty(json)) return;

                NewsList list = null;
                string trimmed = json.Trim();
                
                if (trimmed.StartsWith("{") && trimmed.Contains("\"news\""))
                {
                    // It's already a wrapper! (Either our new InitMessagesWrapper or something similar)
                    list = JsonUtility.FromJson<NewsList>(json);
                }
                else if (trimmed.StartsWith("["))
                {
                    // It's a raw array, use legacy wrapping logic
                    list = JsonUtility.FromJson<NewsList>("{\"news\":" + json + "}");
                }

                if (list == null || list.news == null) 
                {
                    Debug.LogWarning("[News] Could not parse news JSON. Root content might be missing.");
                    return;
                }

                foreach (var item in list.news)
                {
                    if (item != null) CreateNewsItem(item);
                }
            }
            catch (System.Exception e) { Debug.LogError("[News] SetNewsJson Error: " + e.Message); }
        }

        private void CreateNewsItem(NewsData data)
        {
            if (data == null || _container == null) return;

            // 1. Root Container
            GameObject root = new GameObject("NewsItem", typeof(RectTransform));
            root.transform.SetParent(_container, false);
            VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
            rootVlg.childControlHeight = true; rootVlg.childForceExpandHeight = false;
            rootVlg.childControlWidth = true; rootVlg.childForceExpandWidth = true;
            rootVlg.padding = new RectOffset(10, 10, 5, 15);
            rootVlg.spacing = 5;

            // 2. Date
            GameObject dateObj = new GameObject("Date", typeof(Text));
            dateObj.transform.SetParent(root.transform, false);
            Text dateTxt = dateObj.GetComponent<Text>();
            dateTxt.font = Resources.Load<Font>("LegacyRuntime") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dateTxt.text = FormatDate(data.date);
            dateTxt.fontSize = 13;
            dateTxt.color = new Color(0.7f, 0.9f, 0.3f);
            dateTxt.alignment = TextAnchor.MiddleCenter;

            // 3. FrameBox (Main Box for Content + Borders)
            GameObject frameBox = new GameObject("FrameBox", typeof(RectTransform));
            frameBox.transform.SetParent(root.transform, false);
            
            // Layout for content inside the frame
            VerticalLayoutGroup fVlg = frameBox.AddComponent<VerticalLayoutGroup>();
            fVlg.padding = new RectOffset(20, 20, 15, 15);
            fVlg.spacing = 10;
            fVlg.childControlHeight = true; fVlg.childForceExpandHeight = false;
            fVlg.childControlWidth = true; fVlg.childForceExpandWidth = true;
            
            // Background
            Image bg = frameBox.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);
            
            // DRAW THE BORDERS (They will ignore the VerticalLayoutGroup above)
            GreenFrame.Create(frameBox, _frameSprite, _cornerSprite, new Color(0.4f, 1f, 0.4f, 1f));

            // 4. Content (Separator + Image + Header + Body)
            // Separator Line
            GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(frameBox.transform, false);
            sep.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
            sep.GetComponent<Image>().color = new Color(0.4f, 1f, 0.4f, 0.3f); // Subtle green
            sep.AddComponent<LayoutElement>().preferredHeight = 2;

            // Image
            if (!string.IsNullOrEmpty(data.image))
            {
                GameObject imgObj = new GameObject("Img", typeof(Image));
                imgObj.transform.SetParent(frameBox.transform, false);
                Image ni = imgObj.GetComponent<Image>(); ni.preserveAspect = true;
                LayoutElement le = imgObj.AddComponent<LayoutElement>();
                le.preferredWidth = 240; le.preferredHeight = 150;
                StartCoroutine(LoadImage(data.image, ni, le));
            }

            // Header & Text
            GameObject txtObj = new GameObject("Txt", typeof(Text));
            txtObj.transform.SetParent(frameBox.transform, false);
            Text t = txtObj.GetComponent<Text>();
            t.font = dateTxt.font; t.fontSize = 12; t.color = new Color(0.4f, 1f, 0.4f);
            t.alignment = TextAnchor.UpperCenter; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            
            string uname = (_userData != null && _userData.Uid != null) ? _userData.Uid.Value : "Игрок";
            t.text = "<color=#66FF66><b>" + (data.header ?? "").Replace("%USERNAME%", uname) + "</b></color>\n\n" + (data.text ?? "").Replace("%USERNAME%", uname);

            // Magic sizing
            txtObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            frameBox.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private System.Collections.IEnumerator LoadImage(string url, Image target, LayoutElement le)
        {
            if (string.IsNullOrEmpty(url) || target == null) yield break;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (tex != null && target != null)
                    {
                        target.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
                        if (le != null) le.preferredHeight = le.preferredWidth * ((float)tex.height / tex.width);
                    }
                }
            }
        }

        private string FormatDate(string tsStr)
        {
            if (string.IsNullOrEmpty(tsStr)) return "НОВОСТИ";
            if (long.TryParse(tsStr, out long ts))
            {
                if (ts > 1000000000) return new System.DateTime(1970,1,1,0,0,0,System.DateTimeKind.Utc).AddMilliseconds(ts).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }
            return tsStr;
        }
    }
}
