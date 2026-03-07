using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "SnackDatabase", menuName = "SnackAttack/Snack Database")]
    public class SnackDatabaseSO : ScriptableObject
    {
        public List<SnackSO> snacks = new List<SnackSO>();

        [Header("Spawn Settings")]
        public float baseInterval = 1.5f;
        public float minInterval = 0.5f;
        public int maxActive = 10;
        public float acceleration = 0.9f;

        public int Count => snacks.Count;

        public SnackSO GetById(string id)
        {
            for (int i = 0; i < snacks.Count; i++)
            {
                if (snacks[i].id == id)
                    return snacks[i];
            }
            return null;
        }

        public SnackSO GetWeightedRandom()
        {
            return GetWeightedRandomFromPool(null);
        }

        public SnackSO GetWeightedRandomFromPool(SnackSO[] pool)
        {
            var source = pool != null && pool.Length > 0 ? pool : snacks.ToArray();
            if (source.Length == 0) return null;

            float totalWeight = 0f;
            for (int i = 0; i < source.Length; i++)
                totalWeight += source[i].spawnWeight;

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < source.Length; i++)
            {
                cumulative += source[i].spawnWeight;
                if (random <= cumulative)
                    return source[i];
            }
            return source[source.Length - 1];
        }
    }
}
