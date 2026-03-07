using System.Collections.Generic;
using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "SnackAttack/Character Database")]
    public class CharacterDatabaseSO : ScriptableObject
    {
        public List<CharacterSO> characters = new List<CharacterSO>();

        public int Count => characters.Count;

        public CharacterSO GetById(string id)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].id == id)
                    return characters[i];
            }
            return null;
        }
    }
}
