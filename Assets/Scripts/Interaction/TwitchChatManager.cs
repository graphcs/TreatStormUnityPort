using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Interaction
{
    public enum TwitchConnectionState { Disconnected, Connecting, Connected, Error }

    public class TwitchChatManager : MonoBehaviour
    {
        private struct TwitchVote
        {
            public string Username;
            public string Command;
        }

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private Thread _readThread;
        private volatile bool _shouldRun;
        private volatile TwitchConnectionState _state = TwitchConnectionState.Disconnected;
        private string _errorMessage;
        private int _reconnectAttempts;

        private readonly ConcurrentQueue<TwitchVote> _incomingVotes = new();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new();

        private VotingSystem _votingSystem;
        private ChatSimulator _chatSimulator;
        private string[] _activeOptions;

        private TwitchConfigSO _config;
        private string _channel;
        private string _oauthToken;
        private string _botUsername;

        // Regex to parse PRIVMSG: :user!user@user.tmi.twitch.tv PRIVMSG #channel :message
        private static readonly Regex PrivmsgRegex = new(
            @"^:(\w+)!\w+@\w+\.tmi\.twitch\.tv PRIVMSG #\w+ :(.+)$",
            RegexOptions.Compiled);

        public TwitchConnectionState State => _state;
        public bool IsConnected => _state == TwitchConnectionState.Connected;
        public string ErrorMessage => _errorMessage;
        public event Action<TwitchConnectionState> OnStateChanged;

        public void SetConfig(TwitchConfigSO config) => _config = config;

        public void Connect(string channel, string oauthToken, string botUsername)
        {
            if (_state == TwitchConnectionState.Connecting || _state == TwitchConnectionState.Connected)
                Disconnect();

            _channel = channel.ToLower().Trim();
            _oauthToken = oauthToken.Trim();
            _botUsername = string.IsNullOrEmpty(botUsername) ?
                (_config != null ? _config.defaultBotUsername : "SnackAttackBot") :
                botUsername.Trim();
            _reconnectAttempts = 0;
            _errorMessage = null;

            StartConnection();
        }

        public void Disconnect()
        {
            _shouldRun = false;

            try { _writer?.Close(); } catch { }
            try { _reader?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            if (_readThread != null && _readThread.IsAlive)
                _readThread.Join(2000);

            _readThread = null;
            _writer = null;
            _reader = null;
            _client = null;

            SetState(TwitchConnectionState.Disconnected);
        }

        public void SetVotingContext(VotingSystem system, ChatSimulator chat, string[] options)
        {
            _votingSystem = system;
            _chatSimulator = chat;
            _activeOptions = options;
        }

        public void ClearVotingContext()
        {
            _votingSystem = null;
            _chatSimulator = null;
            _activeOptions = null;
        }

        public void TestConnectionAsync(string channel, string token, string username, Action<bool, string> callback)
        {
            var testThread = new Thread(() =>
            {
                TcpClient testClient = null;
                try
                {
                    string server = _config != null ? _config.ircServer : "irc.chat.twitch.tv";
                    int port = _config != null ? _config.ircPort : 6667;

                    testClient = new TcpClient();
                    testClient.Connect(server, port);

                    var stream = testClient.GetStream();
                    var writer = new StreamWriter(stream) { AutoFlush = true };
                    var reader = new StreamReader(stream);

                    string tokenStr = token.Trim();
                    if (!tokenStr.StartsWith("oauth:"))
                        tokenStr = "oauth:" + tokenStr;

                    writer.WriteLine($"PASS {tokenStr}");
                    writer.WriteLine($"NICK {username.ToLower().Trim()}");
                    writer.WriteLine($"JOIN #{channel.ToLower().Trim()}");

                    // Wait for response (up to 5 seconds)
                    stream.ReadTimeout = 5000;
                    bool gotWelcome = false;
                    string errorMsg = null;
                    var deadline = DateTime.UtcNow.AddSeconds(5);

                    while (DateTime.UtcNow < deadline)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        if (line.Contains("Welcome") || line.Contains("001"))
                        {
                            gotWelcome = true;
                            break;
                        }
                        if (line.Contains("Login authentication failed") || line.Contains("NOTICE"))
                        {
                            errorMsg = "Authentication failed. Check OAuth token.";
                            break;
                        }
                    }

                    writer.Close();
                    reader.Close();
                    testClient.Close();

                    if (errorMsg != null)
                        _mainThreadActions.Enqueue(() => callback(false, errorMsg));
                    else if (gotWelcome)
                        _mainThreadActions.Enqueue(() => callback(true, "Connected successfully!"));
                    else
                        _mainThreadActions.Enqueue(() => callback(false, "Timed out waiting for server response."));
                }
                catch (Exception e)
                {
                    try { testClient?.Close(); } catch { }
                    _mainThreadActions.Enqueue(() => callback(false, $"Connection failed: {e.Message}"));
                }
            });
            testThread.IsBackground = true;
            testThread.Start();
        }

        private void StartConnection()
        {
            SetState(TwitchConnectionState.Connecting);
            _shouldRun = true;

            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "TwitchIRC"
            };
            _readThread.Start();
        }

        private void ReadLoop()
        {
            try
            {
                string server = _config != null ? _config.ircServer : "irc.chat.twitch.tv";
                int port = _config != null ? _config.ircPort : 6667;

                _client = new TcpClient();
                _client.Connect(server, port);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                string tokenStr = _oauthToken;
                if (!tokenStr.StartsWith("oauth:"))
                    tokenStr = "oauth:" + tokenStr;

                _writer.WriteLine($"PASS {tokenStr}");
                _writer.WriteLine($"NICK {_botUsername.ToLower()}");
                _writer.WriteLine($"JOIN #{_channel}");

                // Wait for welcome
                bool authenticated = false;
                while (_shouldRun)
                {
                    string line = _reader.ReadLine();
                    if (line == null) break;

                    if (line.Contains("Welcome") || line.Contains("001"))
                    {
                        authenticated = true;
                        SetState(TwitchConnectionState.Connected);
                        _mainThreadActions.Enqueue(() =>
                            EventBus.Emit(GameEvent.TwitchConnected));
                        break;
                    }
                    if (line.Contains("Login authentication failed"))
                    {
                        _errorMessage = "Authentication failed";
                        SetState(TwitchConnectionState.Error);
                        _mainThreadActions.Enqueue(() =>
                            EventBus.Emit(GameEvent.TwitchError));
                        return;
                    }
                }

                if (!authenticated) return;

                // Main read loop
                while (_shouldRun)
                {
                    string line = _reader.ReadLine();
                    if (line == null) break;

                    if (line.StartsWith("PING"))
                    {
                        _writer.WriteLine("PONG :tmi.twitch.tv");
                        continue;
                    }

                    var match = PrivmsgRegex.Match(line);
                    if (match.Success)
                    {
                        string username = match.Groups[1].Value;
                        string message = match.Groups[2].Value.Trim();

                        string prefix = _config != null ? _config.commandPrefix : "!";
                        if (message.StartsWith(prefix))
                        {
                            string command = message.Substring(prefix.Length).Trim().ToLower();
                            _incomingVotes.Enqueue(new TwitchVote
                            {
                                Username = username,
                                Command = command
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!_shouldRun) return; // Expected disconnect

                _errorMessage = e.Message;
                SetState(TwitchConnectionState.Error);
                _mainThreadActions.Enqueue(() =>
                    EventBus.Emit(GameEvent.TwitchError));

                // Attempt reconnect
                int maxAttempts = _config != null ? _config.maxReconnectAttempts : 3;
                float delay = _config != null ? _config.reconnectDelay : 5f;

                while (_shouldRun && _reconnectAttempts < maxAttempts)
                {
                    _reconnectAttempts++;
                    Thread.Sleep((int)(delay * 1000));
                    if (!_shouldRun) return;

                    try
                    {
                        _client?.Close();
                        _client = new TcpClient();
                        string server = _config != null ? _config.ircServer : "irc.chat.twitch.tv";
                        int port = _config != null ? _config.ircPort : 6667;
                        _client.Connect(server, port);

                        var stream = _client.GetStream();
                        _writer = new StreamWriter(stream) { AutoFlush = true };
                        _reader = new StreamReader(stream);

                        string tokenStr = _oauthToken;
                        if (!tokenStr.StartsWith("oauth:"))
                            tokenStr = "oauth:" + tokenStr;

                        _writer.WriteLine($"PASS {tokenStr}");
                        _writer.WriteLine($"NICK {_botUsername.ToLower()}");
                        _writer.WriteLine($"JOIN #{_channel}");

                        SetState(TwitchConnectionState.Connecting);

                        // Re-enter main loop via recursion would be messy; just return and let Update handle
                        // Actually, let's read for welcome inline
                        bool reconnected = false;
                        while (_shouldRun)
                        {
                            string line = _reader.ReadLine();
                            if (line == null) break;
                            if (line.Contains("Welcome") || line.Contains("001"))
                            {
                                reconnected = true;
                                SetState(TwitchConnectionState.Connected);
                                _mainThreadActions.Enqueue(() =>
                                    EventBus.Emit(GameEvent.TwitchConnected));
                                break;
                            }
                            if (line.Contains("Login authentication failed"))
                                return;
                        }

                        if (!reconnected) continue;

                        // Resume read loop
                        while (_shouldRun)
                        {
                            string line = _reader.ReadLine();
                            if (line == null) break;

                            if (line.StartsWith("PING"))
                            {
                                _writer.WriteLine("PONG :tmi.twitch.tv");
                                continue;
                            }

                            var match = PrivmsgRegex.Match(line);
                            if (match.Success)
                            {
                                string username = match.Groups[1].Value;
                                string message = match.Groups[2].Value.Trim();
                                string prefix = _config != null ? _config.commandPrefix : "!";
                                if (message.StartsWith(prefix))
                                {
                                    string command = message.Substring(prefix.Length).Trim().ToLower();
                                    _incomingVotes.Enqueue(new TwitchVote
                                    {
                                        Username = username,
                                        Command = command
                                    });
                                }
                            }
                        }
                        return; // Clean exit from reconnect loop
                    }
                    catch
                    {
                        // Continue retry loop
                    }
                }

                // All retries exhausted
                if (_shouldRun)
                {
                    _errorMessage = "Max reconnect attempts reached";
                    SetState(TwitchConnectionState.Error);
                    _mainThreadActions.Enqueue(() =>
                    {
                        EventBus.Emit(GameEvent.TwitchDisconnected);
                        EventBus.Emit(GameEvent.TwitchError);
                    });
                }
            }
        }

        private void SetState(TwitchConnectionState newState)
        {
            if (_state == newState) return;
            _state = newState;
            _mainThreadActions.Enqueue(() => OnStateChanged?.Invoke(newState));
        }

        private void Update()
        {
            // Process main thread callbacks
            while (_mainThreadActions.TryDequeue(out var action))
                action?.Invoke();

            // Drain incoming votes
            while (_incomingVotes.TryDequeue(out var vote))
                ProcessVote(vote);
        }

        private void ProcessVote(TwitchVote vote)
        {
            if (_activeOptions == null || _votingSystem == null) return;

            int optionIndex = -1;
            for (int i = 0; i < _activeOptions.Length; i++)
            {
                if (string.Equals(vote.Command, _activeOptions[i], StringComparison.OrdinalIgnoreCase))
                {
                    optionIndex = i;
                    break;
                }
            }

            if (optionIndex < 0) return;

            _votingSystem.AddVote(vote.Username, optionIndex);

            if (_chatSimulator != null)
            {
                Color msgColor = _config != null ? _config.twitchMessageColor : new Color(0.6f, 0.3f, 0.9f);
                string prefix = _config != null ? _config.commandPrefix : "!";
                _chatSimulator.AddMessage(vote.Username, $"{prefix}{vote.Command}", msgColor);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
