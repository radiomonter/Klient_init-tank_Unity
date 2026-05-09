using UnityEngine;
using UnityEngine.UI;

namespace Tanki.UI
{
    [RequireComponent(typeof(Image))]
    public class TankWindow : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Sprite _headerSprite;
        [SerializeField] private Sprite _bgTileSprite;
        
        [Header("Components")]
        [SerializeField] private Image _headerImage;
        [SerializeField] private Image _bgImage;

        private void OnValidate()
        {
            Setup();
        }

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            if (_bgImage == null) _bgImage = GetComponent<Image>();
            
            if (_bgImage != null)
            {
                _bgImage.type = Image.Type.Sliced;
                // Note: The Sprite must have correct border settings in Unity (9-slice)
            }

            if (_headerImage != null && _headerSprite != null)
            {
                _headerImage.sprite = _headerSprite;
                _headerImage.SetNativeSize();
            }
        }
    }
}
