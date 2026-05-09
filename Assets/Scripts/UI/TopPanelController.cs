using UnityEngine;
using UnityEngine.UI;
using Tanki.Core.Variables;

namespace Tanki.UI
{
    public class TopPanelController : MonoBehaviour
    {
        [Header("Data References")]
        [SerializeField] private StringVariable _uid;
        [SerializeField] private IntVariable _rank;
        [SerializeField] private IntVariable _crystals;
        [SerializeField] private IntVariable _score;
        [SerializeField] private IntVariable _nextRankScore;

        [Header("UI Elements")]
        [SerializeField] private Text _uidText;
        [SerializeField] private Image _rankIcon;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Slider _rankProgress;

        [Header("Resources")]
        [SerializeField] private Sprite[] _rankSprites;

        private void OnEnable()
        {
            if (_uid != null) _uid.OnValueChanged += UpdateUid;
            if (_rank != null) _rank.OnValueChanged += UpdateRank;
            if (_crystals != null) _crystals.OnValueChanged += UpdateCrystals;
            if (_score != null) _score.OnValueChanged += UpdateProgress;
            if (_nextRankScore != null) _nextRankScore.OnValueChanged += UpdateProgress;

            // Initial refresh
            if (_uid != null) UpdateUid(_uid.Value);
            if (_rank != null) UpdateRank(_rank.Value);
            if (_crystals != null) UpdateCrystals(_crystals.Value);
            UpdateProgress(0);
        }

        private void OnDisable()
        {
            if (_uid != null) _uid.OnValueChanged -= UpdateUid;
            if (_rank != null) _rank.OnValueChanged -= UpdateRank;
            if (_crystals != null) _crystals.OnValueChanged -= UpdateCrystals;
            if (_score != null) _score.OnValueChanged -= UpdateProgress;
            if (_nextRankScore != null) _nextRankScore.OnValueChanged -= UpdateProgress;
        }

        private void UpdateUid(string value) => _uidText.text = value;
        
        private void UpdateRank(int value)
        {
            if (_rankSprites != null && value >= 0 && value < _rankSprites.Length)
            {
                _rankIcon.sprite = _rankSprites[value];
                _rankIcon.SetNativeSize();
            }
        }

        private void UpdateCrystals(int value) => _crystalsText.text = value.ToString("N0");
        
        private void UpdateProgress(int _)
        {
            if (_score == null || _nextRankScore == null) return;
            
            if (_scoreText != null)
                _scoreText.text = $"{_score.Value} / {_nextRankScore.Value}";
                
            if (_rankProgress != null && _nextRankScore.Value > 0)
            {
                _rankProgress.value = (float)_score.Value / _nextRankScore.Value;
            }
        }
    }
}
