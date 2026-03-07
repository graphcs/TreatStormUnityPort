using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "SnackAttack/Level Database")]
    public class LevelDatabaseSO : ScriptableObject
    {
        public List<LevelSO> levels = new List<LevelSO>();

        public int Count => levels.Count;

        public LevelSO GetByNumber(int levelNumber)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].levelNumber == levelNumber)
                    return levels[i];
            }
            return null;
        }
    }
}
