using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanki.Core
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static UnityMainThreadDispatcher _instance = null;
        private static bool _isQuitting = false;

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    if (_instance == null)
                    {
                        var obj = new GameObject("MainThreadDispatcher");
                        _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        public void Enqueue(Action action)
        {
            EnqueueAction(action);
        }

        public static void EnqueueAction(Action action)
        {
            Debug.Log("[Dispatcher] Enqueuing action...");
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    Debug.Log("[Dispatcher] Dequeuing and executing action...");
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}
