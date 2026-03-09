using System;
using System.Collections.Generic;

namespace SnackAttack.Interaction
{
    public enum VoteMode { Action, Treat, Trivia }
    public enum VotePhase { Idle, Voting, Cooldown }

    public class VotingSystem
    {
        private VotePhase _phase = VotePhase.Idle;
        private VoteMode _mode;
        private float _timer;
        private string[] _options;
        private int[] _voteCounts;
        private readonly Dictionary<string, int> _voterToOption = new();
        private int _winnerIndex = -1;
        private int _triviaCorrectIndex = -1;
        private string _correctTriviaAnswer;

        private float _votingDuration;
        private float _cooldownDuration;

        // Single vote mode: voting happens once then deactivates after cooldown
        private bool _singleVoteMode;
        public bool SingleVoteCompleted { get; private set; }

        // Callbacks
        public Action<int, string> OnVotingResolved;
        public Action OnVotingStarted;
        public Action OnCooldownExpired;

        // Read-only state
        public VotePhase Phase => _phase;
        public VoteMode Mode => _mode;
        public float TimeRemaining => _timer;
        public string[] Options => _options;
        public int[] VoteCounts => _voteCounts;
        public int TotalVotes
        {
            get
            {
                if (_voteCounts == null) return 0;
                int total = 0;
                for (int i = 0; i < _voteCounts.Length; i++)
                    total += _voteCounts[i];
                return total;
            }
        }
        public int WinnerIndex => _winnerIndex;
        public int TriviaCorrectIndex => _triviaCorrectIndex;
        public string CorrectTriviaAnswer => _correctTriviaAnswer;

        public VotingSystem(float votingDuration, float cooldownDuration)
        {
            _votingDuration = votingDuration;
            _cooldownDuration = cooldownDuration;
        }

        /// <summary>
        /// Configure the voting mode and options without starting the voting window.
        /// Call StartVotingWindow() separately to begin voting.
        /// </summary>
        public void SetMode(VoteMode mode, string[] options, string correctAnswer = null,
            bool singleVoteMode = false)
        {
            _mode = mode;
            _options = options;
            _correctTriviaAnswer = correctAnswer;
            _singleVoteMode = singleVoteMode;
            SingleVoteCompleted = false;
            _voteCounts = new int[options.Length];
            _voterToOption.Clear();
            _winnerIndex = -1;
            _triviaCorrectIndex = -1;
            _timer = _votingDuration;
            _phase = VotePhase.Idle;

            // Find triviaCorrectIndex from correctAnswer
            if (correctAnswer != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    if (string.Equals(options[i], correctAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        _triviaCorrectIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Start a fresh voting window for the currently configured mode.
        /// </summary>
        public void StartVotingWindow()
        {
            _voteCounts = new int[_options.Length];
            _voterToOption.Clear();
            _winnerIndex = -1;
            SingleVoteCompleted = false;
            _timer = _votingDuration;
            _phase = VotePhase.Voting;

            OnVotingStarted?.Invoke();
        }

        public bool AddVote(string voterId, int optionIdx)
        {
            if (_phase != VotePhase.Voting) return false;
            if (optionIdx < 0 || optionIdx >= _options.Length) return false;

            // If voter already voted, change their vote
            if (_voterToOption.TryGetValue(voterId, out int prevIdx))
            {
                _voteCounts[prevIdx]--;
            }

            _voterToOption[voterId] = optionIdx;
            _voteCounts[optionIdx]++;
            return true;
        }

        public void Update(float dt)
        {
            if (_phase == VotePhase.Idle) return;

            _timer -= dt;

            if (_phase == VotePhase.Voting && _timer <= 0f)
            {
                ResolveVoting();
            }
            else if (_phase == VotePhase.Cooldown && _timer <= 0f)
            {
                if (_singleVoteMode)
                {
                    // Single vote mode: deactivate after cooldown
                    _phase = VotePhase.Idle;
                    SingleVoteCompleted = true;
                }
                else
                {
                    // Cycling mode: reset votes and start new voting window
                    _voteCounts = new int[_options.Length];
                    _voterToOption.Clear();
                    _winnerIndex = -1;
                    _timer = _votingDuration;
                    _phase = VotePhase.Voting;
                }
                OnCooldownExpired?.Invoke();
            }
        }

        public void Reset()
        {
            _phase = VotePhase.Idle;
            _options = null;
            _voteCounts = null;
            _voterToOption.Clear();
            _winnerIndex = -1;
            _triviaCorrectIndex = -1;
            _correctTriviaAnswer = null;
            SingleVoteCompleted = false;
        }

        private void ResolveVoting()
        {
            // Find winner (ties → first option wins, matching PyGame sort by -count then index)
            int maxVotes = -1;
            _winnerIndex = 0;
            for (int i = 0; i < _voteCounts.Length; i++)
            {
                if (_voteCounts[i] > maxVotes)
                {
                    maxVotes = _voteCounts[i];
                    _winnerIndex = i;
                }
            }

            string winnerOption = _options[_winnerIndex];
            _timer = _cooldownDuration;
            _phase = VotePhase.Cooldown;

            OnVotingResolved?.Invoke(_winnerIndex, winnerOption);
        }
    }
}
