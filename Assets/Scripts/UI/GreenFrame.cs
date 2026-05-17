using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    public class GreenFrame : MonoBehaviour
    {
        public static GameObject Create(GameObject parent, Sprite frameSprite, Sprite cornerSprite, Color color)
        {
            GameObject frame = new GameObject("GreenFrame", typeof(RectTransform));
            frame.transform.SetParent(parent.transform, false);
            frame.AddComponent<LayoutElement>().ignoreLayout = true; // Crucial: don't let LayoutGroups move the frame
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

            float f = 2; // Stable thin thickness
            float h = f / 2f; 
            Debug.Log("[GreenFrame.cs] Creating ignored frame with f=2");

            // Corners - Using centered pivot for stable rotation
            CreatePart(frame, "TL", cornerSprite, new Vector2(0, 1), new Vector2(h, -h), new Vector2(f, f), 0, color);
            CreatePart(frame, "TR", cornerSprite, new Vector2(1, 1), new Vector2(-h, -h), new Vector2(f, f), -90, color);
            CreatePart(frame, "BR", cornerSprite, new Vector2(1, 0), new Vector2(-h, h), new Vector2(f, f), -180, color);
            CreatePart(frame, "BL", cornerSprite, new Vector2(0, 0), new Vector2(h, h), new Vector2(f, f), -270, color);

            // Lines - Responsive stretch
            CreateLine(frame, "T", frameSprite, new Vector2(0, 1), new Vector2(1, 1), new Vector2(f, -f), new Vector2(-f, 0), color);
            CreateLine(frame, "B", frameSprite, new Vector2(0, 0), new Vector2(1, 0), new Vector2(f, 0), new Vector2(-f, f), color);
            CreateLine(frame, "L", frameSprite, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, f), new Vector2(f, -f), color);
            CreateLine(frame, "R", frameSprite, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-f, f), new Vector2(0, -f), color);

            return frame;
        }

        private static void CreatePart(GameObject parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, float rot, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; 
            rt.pivot = new Vector2(0.5f, 0.5f); // Critical for rotation symmetry
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            rt.localEulerAngles = new Vector3(0, 0, rot);
            Image img = obj.GetComponent<Image>();
            img.sprite = sprite; img.color = color;
            img.raycastTarget = false;
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private static void CreateLine(GameObject parent, string name, Sprite sprite, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            Image img = obj.GetComponent<Image>();
            img.sprite = sprite; img.color = color; 
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            obj.AddComponent<LayoutElement>().ignoreLayout = true;
        }
    }
}
