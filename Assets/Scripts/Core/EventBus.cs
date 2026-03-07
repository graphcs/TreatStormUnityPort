using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    public static class EventBus
    {
        private struct Subscriber
        {
            public Action<EventData> callback;
            public int priority;
        }

        private static readonly Dictionary<GameEvent, List<Subscriber>> _listeners = new();
        private static readonly List<EventData> _queue = new();

        public static void Subscribe(GameEvent eventType, Action<EventData> callback, int priority = 0)
        {
            if (!_listeners.TryGetValue(eventType, out var list))
            {
                list = new List<Subscriber>();
                _listeners[eventType] = list;
            }

            list.Add(new Subscriber { callback = callback, priority = priority });

            // Sort descending by priority (higher priority first)
            list.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        public static void Unsubscribe(GameEvent eventType, Action<EventData> callback)
        {
            if (!_listeners.TryGetValue(eventType, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].callback == callback)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        public static void Emit(GameEvent type, Dictionary<string, object> payload = null, string source = "system")
        {
            var data = new EventData
            {
                type = type,
                payload = payload,
                timestamp = Time.time,
                source = source
            };
            Emit(type, data);
        }

        public static void Emit(GameEvent type, EventData data)
        {
            if (!_listeners.TryGetValue(type, out var list))
                return;

            // Copy list to tolerate subscribe/unsubscribe during dispatch
            var snapshot = new List<Subscriber>(list);

            foreach (var subscriber in snapshot)
            {
                try
                {
                    subscriber.callback(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Error in {type} handler: {e}");
                }
            }
        }

        public static void Enqueue(GameEvent type, Dictionary<string, object> payload = null, string source = "system")
        {
            Enqueue(new EventData
            {
                type = type,
                payload = payload,
                timestamp = Time.time,
                source = source
            });
        }

        public static void Enqueue(EventData data)
        {
            _queue.Add(data);
        }

        public static void ProcessQueue()
        {
            if (_queue.Count == 0)
                return;

            // Snapshot and clear before processing
            var snapshot = new List<EventData>(_queue);
            _queue.Clear();

            foreach (var data in snapshot)
            {
                Emit(data.type, data);
            }
        }

        public static void Clear()
        {
            _listeners.Clear();
            _queue.Clear();
        }

        public static void ClearListeners(GameEvent eventType)
        {
            if (_listeners.ContainsKey(eventType))
                _listeners[eventType].Clear();
        }
    }
}
