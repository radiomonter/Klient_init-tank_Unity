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
        
        [System.Serializable]
        public class NewsData
        {
            public string image;
            public string date;
            public string header;
            public string id;
            public string text;
        }

        [System.Serializable]
        public class NewsList
        {
            public NewsData[] news;
        }

        public void SetNewsJson(string json)
        {
            try
            {
                // Clear old news
                foreach (Transform child in _container)
                {
                    Destroy(child.gameObject);
                }

                NewsList list = JsonUtility.FromJson<NewsList>("{\"news\":" + json + "}");
                if (list == null || list.news == null) return;

                foreach (var item in list.news)
                {
                    CreateNewsItem(item);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[News] Error parsing JSON: " + e.Message);
            }
        }

        private void CreateNewsItem(NewsData data)
        {
            // Root for the item
            GameObject root = new GameObject("NewsItem_" + data.id, typeof(RectTransform));
            root.transform.SetParent(_container, false);
            VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
            rootVlg.childControlHeight = true;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childControlWidth = true;
            rootVlg.childForceExpandWidth = true;
            rootVlg.spacing = 4;

            // Date Header
            GameObject dateObj = new GameObject("NewsDate_Label", typeof(RectTransform));
            dateObj.transform.SetParent(root.transform, false);
            Text dateTxt = dateObj.AddComponent<Text>();
            // Use LegacyRuntime or default font safely
            Font legacyFont = Resources.Load<Font>("LegacyRuntime");
            if (legacyFont == null) legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            dateTxt.font = legacyFont;
            dateTxt.text = FormatDate(data.date);
            dateTxt.fontSize = 14; 
            dateTxt.fontStyle = FontStyle.Bold;
            dateTxt.color = new Color(1f, 0.95f, 0.5f); // Bright yellow-gold
            dateTxt.alignment = TextAnchor.MiddleLeft;
            dateObj.AddComponent<LayoutElement>().minHeight = 25;
            
            if (dateObj.GetComponent<Image>() != null) DestroyImmediate(dateObj.GetComponent<Image>());
            Debug.Log($"[News] Created date item: '{dateTxt.text}' (raw: '{data.date}')");

            // Green Frame Box
            GameObject boxObj = new GameObject("ContentBox", typeof(RectTransform));
            boxObj.transform.SetParent(root.transform, false);
            
            // Add semi-transparent neutral background
            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.color = new Color(0.12f, 0.12f, 0.12f, 0.4f); 
            boxBg.type = Image.Type.Sliced;
            
            GreenFrame.Create(boxObj, _frameSprite, _cornerSprite);
            
            VerticalLayoutGroup vlg = boxObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            // Image
            if (!string.IsNullOrEmpty(data.image))
            {
                GameObject imgObj = new GameObject("Image", typeof(RectTransform));
                imgObj.transform.SetParent(boxObj.transform, false);
                Image newsImg = imgObj.AddComponent<Image>();
                newsImg.preserveAspect = true;
                LayoutElement le = imgObj.AddComponent<LayoutElement>();
                le.preferredWidth = 200;
                le.preferredHeight = 120;
                StartCoroutine(LoadImage(data.image, newsImg, le));
            }

            // Header/Body Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(boxObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.font = dateTxt.font;
            
            string username = (_userData != null && _userData.Uid != null) ? _userData.Uid.Value : "Игрок";
            string processedHeader = data.header.Replace("%USERNAME%", username);
            string processedText = data.text.Replace("%USERNAME%", username);
            
            txt.text = "<size=15><b>" + processedHeader + "</b></size>\n\n" + processedText;
            txt.fontSize = 13; 
            txt.color = new Color(0.8f, 1f, 0.5f); // Light green text
            txt.alignment = TextAnchor.UpperCenter; // Start from top
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate; // Better for layout consistency
            txt.supportRichText = true;
            
            textObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Ensure box expands to fit the text
            boxObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private System.Collections.IEnumerator LoadImage(string url, Image target, LayoutElement le)
        {
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    target.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    float aspect = (float)texture.height / texture.width;
                    le.preferredHeight = le.preferredWidth * aspect;
                }
            }
        }

        private string FormatDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate)) return "НОВОСТИ";
            if (long.TryParse(rawDate, out long timestamp))
            {
                if (timestamp > 1000000000) 
                {
                    System.DateTime dt = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(timestamp);
                    return dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                }
            }
            if (rawDate.Length < 3) 
            {
                // Try to see if it's a number anyway
                if (int.TryParse(rawDate, out int num)) return "Новость #" + num;
                return "НОВОСТИ";
            }
            return rawDate;
        }
    }
}
