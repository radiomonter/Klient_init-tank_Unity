using UnityEngine;
using System;

namespace Tanki.Core.Variables
{
    [CreateAssetMenu(fileName = "New Int Variable", menuName = "Tanki/Core/Variables/Int")]
    public class IntVariable : ScriptableObject
    {
        [SerializeField] private int _value;

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnValueChanged?.Invoke(value);
                }
            }
        }

        public event Action<int> OnValueChanged;

        public void SetValue(int value) => Value = value;
        public void ApplyChange(int amount) => Value += amount;
    }
}
