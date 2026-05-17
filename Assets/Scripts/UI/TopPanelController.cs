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
        [SerializeField] private BoolVariable _isPremium;

        [Header("UI Elements")]
        [SerializeField] private Text _uidText;
        [SerializeField] private Image _rankIcon;
        [SerializeField] private Text _crystalsText;
        [SerializeField] private Text _scoreText;

        [Header("Progress Bar (Sprite-based)")]
        [SerializeField] private RectTransform _progressFillContainer;

        [Header("Resources")]
        [SerializeField] private Sprite[] _rankSprites;
        [SerializeField] private Sprite[] _premiumRankSprites;

        private void OnEnable()
        {
            if (_uid != null) _uid.OnValueChanged += UpdateUid;
            if (_rank != null) _rank.OnValueChanged += UpdateRank;
            if (_crystals != null) _crystals.OnValueChanged += UpdateCrystals;
            if (_score != null) _score.OnValueChanged += UpdateProgress;
            if (_nextRankScore != null) _nextRankScore.OnValueChanged += UpdateProgress;
            if (_isPremium != null) _isPremium.OnValueChanged += UpdatePremium;

            // Initial refresh
            if (_uid != null) UpdateUid(_uid.Value);
            if (_rank != null) UpdateRank(_rank.Value);
            if (_crystals != null) UpdateCrystals(_crystals.Value);
            if (_isPremium != null) UpdatePremium(_isPremium.Value);
            UpdateProgress(0);
        }

        private void OnDisable()
        {
            if (_uid != null) _uid.OnValueChanged -= UpdateUid;
            if (_rank != null) _rank.OnValueChanged -= UpdateRank;
            if (_crystals != null) _crystals.OnValueChanged -= UpdateCrystals;
            if (_score != null) _score.OnValueChanged -= UpdateProgress;
            if (_nextRankScore != null) _nextRankScore.OnValueChanged -= UpdateProgress;
            if (_isPremium != null) _isPremium.OnValueChanged -= UpdatePremium;
        }

        private void UpdateUid(string value) => RefreshAllText();
        
        private void UpdateRank(int value)
        {
            bool isPremium = _isPremium != null && _isPremium.Value;
            Sprite[] sprites = isPremium ? _premiumRankSprites : _rankSprites;

            if (sprites != null && value > 0 && value <= sprites.Length && sprites[value - 1] != null)
            {
                _rankIcon.sprite = sprites[value - 1];
            }
            RefreshAllText();
        }

        private void UpdatePremium(bool val) => UpdateRank(_rank != null ? _rank.Value : 0);

        private void UpdateCrystals(int value) => _crystalsText.text = value.ToString("N0");
        
        private void UpdateProgress(int _) => RefreshAllText();

        private void RefreshAllText()
        {
            if (_scoreText == null) return;

            string scoreStr = (_score != null && _nextRankScore != null) ? $"{_score.Value} / {_nextRankScore.Value}" : "";
            string rankStr = (_rank != null) ? GetRankName(_rank.Value) : "";
            string uidStr = (_uid != null) ? _uid.Value : "";

            _scoreText.text = $"{scoreStr} {rankStr} {uidStr}".Trim();

            // Handle progress bar fill
            if (_progressFillContainer != null && _nextRankScore != null && _nextRankScore.Value > 0)
            {
                float ratio = Mathf.Clamp01((float)_score.Value / _nextRankScore.Value);
                _progressFillContainer.anchorMax = new Vector2(ratio, _progressFillContainer.anchorMax.y);
            }
        }

        private string GetRankName(int rankIndex)
        {
            // Упрощенный список названий рангов (можно расширить)
            string[] ranks = { "Новобранец", "Рядовой", "Ефрейтор", "Капрал", "Мастер-капрал", "Сержант", "Штаб-сержант", "Мастер-сержант", "Первый сержант", "Сержант-майор", "Уорэнт-офицер 1", "Уорэнт-офицер 2", "Уорэнт-офицер 3", "Уорэнт-офицер 4", "Уорэнт-офицер 5", "Младший лейтенант", "Лейтенант", "Старший лейтенант", "Капитан", "Майор", "Подполковник", "Полковник", "Бригадир", "Генерал-майор", "Генерал-лейтенант", "Генерал", "Маршал", "Фельдмаршал", "Командор", "Генералиссимус" };
            if (rankIndex > 0 && rankIndex <= ranks.Length) return ranks[rankIndex - 1];
            return "";
        }
    }
}
