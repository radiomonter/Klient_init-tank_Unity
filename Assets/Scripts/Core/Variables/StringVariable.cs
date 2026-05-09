using UnityEngine;
using System;

namespace Tanki.Core.Variables
{
    [CreateAssetMenu(fileName = "New String Variable", menuName = "Tanki/Core/Variables/String")]
    public class StringVariable : ScriptableObject
    {
        [SerializeField] private string _value;

        public string Value
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

        public event Action<string> OnValueChanged;

        public void SetValue(string value) => Value = value;
    }
}
