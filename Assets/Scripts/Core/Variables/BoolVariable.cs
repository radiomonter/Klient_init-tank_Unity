using UnityEngine;
using System;

namespace Tanki.Core.Variables
{
    [CreateAssetMenu(fileName = "New Bool Variable", menuName = "Tanki/Core/Variables/Bool")]
    public class BoolVariable : ScriptableObject
    {
        [SerializeField] private bool _value;

        public bool Value
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

        public event Action<bool> OnValueChanged;

        public void SetValue(bool value) => Value = value;
        public void Toggle() => Value = !Value;
    }
}
