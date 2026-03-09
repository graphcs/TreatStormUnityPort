using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    public struct EventData
    {
        public GameEvent type;
        public Dictionary<string, object> payload;
        public float timestamp;
        public string source;

        public static EventData Create(GameEvent type, string source = null)
        {
            return new EventData
            {
                type = type,
                payload = null,
                timestamp = Time.time,
                source = source
            };
        }

        public static EventData Create(GameEvent type, Dictionary<string, object> payload, string source = null)
        {
            return new EventData
            {
                type = type,
                payload = payload,
                timestamp = Time.time,
                source = source
            };
        }

        public object this[string key]
        {
            get
            {
                if (payload != null && payload.TryGetValue(key, out var value))
                    return value;
                return null;
            }
            set
            {
                payload ??= new Dictionary<string, object>();
                payload[key] = value;
            }
        }
    }
}
