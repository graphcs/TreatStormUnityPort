using UnityEngine;

namespace SnackAttack.Core
{
    [CreateAssetMenu(fileName = "TwitchConfig", menuName = "SnackAttack/TwitchConfig")]
    public class TwitchConfigSO : ScriptableObject
    {
        [Header("IRC Connection")]
        public string ircServer = "irc.chat.twitch.tv";
        public int ircPort = 6667;
        public string commandPrefix = "!";
        public string defaultBotUsername = "SnackAttackBot";

        [Header("Reconnection")]
        public float reconnectDelay = 5f;
        public int maxReconnectAttempts = 3;

        [Header("UI Colors")]
        public Color connectedColor = new Color(0.3f, 0.9f, 0.3f);
        public Color disconnectedColor = new Color(0.9f, 0.3f, 0.3f);
        public Color connectingColor = new Color(0.9f, 0.9f, 0.3f);
        public Color twitchMessageColor = new Color(0.6f, 0.3f, 0.9f);
    }
}
