using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    public class GreenFrame : MonoBehaviour
    {
        private const string ASSET_PATH = "Assets/Textures/UI/images/";

        public static GameObject Create(GameObject parent, Sprite frameSprite, Sprite cornerSprite)
        {
            GameObject frame = new GameObject("GreenFrame", typeof(RectTransform));
            frame.transform.SetParent(parent.transform, false);
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

            float f = 16; 
            CreatePart(frame, "TL", cornerSprite, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(f/2, -f/2), new Vector2(f, f), Image.Type.Simple, 0);
            CreatePart(frame, "TR", cornerSprite, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-f/2, -f/2), new Vector2(f, f), Image.Type.Simple, -90);
            CreatePart(frame, "BR", cornerSprite, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-f/2, f/2), new Vector2(f, f), Image.Type.Simple, -180);
            CreatePart(frame, "BL", cornerSprite, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(f/2, f/2), new Vector2(f, f), Image.Type.Simple, -270);

            CreatePart(frame, "Top", frameSprite, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -f/2), new Vector2(-f*2, f), Image.Type.Tiled);
            CreatePart(frame, "Bottom", frameSprite, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, f/2), new Vector2(-f*2, f), Image.Type.Tiled, 180);
            CreatePart(frame, "Left", frameSprite, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(f/2, 0), new Vector2(f, -f*2), Image.Type.Tiled, 90);
            CreatePart(frame, "Right", frameSprite, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-f/2, 0), new Vector2(f, -f*2), Image.Type.Tiled, -90);

            // Anchors for dynamic sizing
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

        private static GameObject CreatePart(GameObject parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Image.Type type, float rotation = 0)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos; rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0, 0, rotation);
            
            Image img = obj.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = type;
            }
            else
            {
                // Fallback to dark green for frame parts
                img.color = new Color(0.0f, 0.15f, 0.0f, 0.8f);
            }
            return obj;
        }
    }
}
