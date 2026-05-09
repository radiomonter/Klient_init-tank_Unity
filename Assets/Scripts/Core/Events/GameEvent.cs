using System.Collections.Generic;
using UnityEngine;

namespace Tanki.Core.Events
{
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Tanki/Core/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<IGameEventListener> _listeners = new List<IGameEventListener>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(IGameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(IGameEventListener listener)
        {
            if (_listeners.Contains(listener))
                _listeners.Remove(listener);
        }
    }

    public interface IGameEventListener
    {
        void OnEventRaised();
    }
}
