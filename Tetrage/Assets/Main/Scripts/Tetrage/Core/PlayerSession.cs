using System;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Contracts;
using Tetrage.Core.DTO;
using Tetrage.Core.Enums;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core
{
    /// <summary>
    /// Photonなどの通信ライブラリに依存しないプレイヤーセッション管理。
    /// </summary>
    public sealed class PlayerSession : IPlayerSession
    {
        #region Fields
        private readonly Dictionary<int, SessionPlayerData> _participants = new Dictionary<int, SessionPlayerData>();
        private int _nextSessionId = 1;
        private int _localSessionId = -1;
        #endregion

        #region Properties
        public IReadOnlyList<SessionPlayerData> Participants => _participants.Values.OrderBy(x => x.SessionId).ToList();

        public SessionPlayerData LocalPlayer
        {
            get
            {
                if (_localSessionId < 0)
                {
                    return null;
                }

                if (_participants.TryGetValue(_localSessionId, out var localPlayer))
                {
                    return localPlayer;
                }

                _localSessionId = -1;
                return null;
            }
        }

        public int ParticipantCount => _participants.Count;
        #endregion

        #region Events
        public event Action<SessionPlayerData> OnParticipantAdded;
        public event Action<SessionPlayerData> OnParticipantUpdated;
        public event Action<int> OnParticipantRemoved;
        public event Action OnSessionCleared;
        #endregion

        #region Session Operations
        public void RegisterLocalPlayer(string name, int iconIndex)
        {
            if (_localSessionId >= 0 && _participants.TryGetValue(_localSessionId, out var existingLocal))
            {
                var updated = existingLocal with
                {
                    PlayerName = NormalizePlayerName(name),
                    IconIndex = iconIndex,
                    IsLocal = true,
                };

                _participants[_localSessionId] = updated;
                OnParticipantUpdated?.Invoke(updated);
                return;
            }

            var sessionId = AddParticipant(name, iconIndex, true);
            _localSessionId = sessionId;
        }

        public int AddParticipant(string name, int iconIndex, bool isLocal = false)
        {
            var sessionId = _nextSessionId++;
            var participant = new SessionPlayerData
            {
                SessionId = sessionId,
                PlayerName = NormalizePlayerName(name),
                IconIndex = iconIndex,
                IsLocal = isLocal,
            };

            _participants[sessionId] = participant;
            if (isLocal)
            {
                _localSessionId = sessionId;
            }

            OnParticipantAdded?.Invoke(participant);
            return sessionId;
        }

        public bool RemoveParticipant(int sessionId)
        {
            if (!_participants.Remove(sessionId))
            {
                return false;
            }

            if (_localSessionId == sessionId)
            {
                _localSessionId = -1;
            }

            OnParticipantRemoved?.Invoke(sessionId);
            return true;
        }

        public void UpdateParticipant(int sessionId, string name = null, int? iconIndex = null, bool? isLocal = null)
        {
            if (!_participants.TryGetValue(sessionId, out var existing))
            {
                return;
            }

            var updated = existing with
            {
                PlayerName = name == null ? existing.PlayerName : NormalizePlayerName(name),
                IconIndex = iconIndex ?? existing.IconIndex,
                IsLocal = isLocal ?? existing.IsLocal,
            };

            _participants[sessionId] = updated;
            if (updated.IsLocal)
            {
                _localSessionId = sessionId;
            }
            else if (_localSessionId == sessionId)
            {
                _localSessionId = -1;
            }

            OnParticipantUpdated?.Invoke(updated);
        }

        public void Clear(bool preserveLocalPlayer = true)
        {
            if (preserveLocalPlayer && LocalPlayer != null)
            {
                var localSnapshot = LocalPlayer;
                _participants.Clear();
                _participants[localSnapshot.SessionId] = localSnapshot with { IsLocal = true };
                _localSessionId = localSnapshot.SessionId;
            }
            else
            {
                _participants.Clear();
                _localSessionId = -1;
            }

            OnSessionCleared?.Invoke();
        }

        public List<PlayerInfo> BuildPlayerInfos(out IPlayerIdMapper playerIdMapper, IReadOnlyDictionary<int, int> sessionIdToActorNumberMap = null)
        {
            var mapper = new PlayerIdMapper();
            var list = new List<PlayerInfo>();
            var orderedParticipants = _participants.Values.OrderBy(x => x.SessionId).ToList();

            for (int i = 0; i < orderedParticipants.Count; i++)
            {
                var participant = orderedParticipants[i];
                var playerId = new PlayerId(i + 1);

                var actorNumber = participant.SessionId;
                if (sessionIdToActorNumberMap != null &&
                    sessionIdToActorNumberMap.TryGetValue(participant.SessionId, out var mappedActorNumber))
                {
                    actorNumber = mappedActorNumber;
                }

                mapper.Register(playerId, actorNumber);

                var info = new PlayerInfo
                {
                    Id = playerId,
                    UserId = NormalizePlayerName(participant.PlayerName),
                    PlayerType = participant.IsLocal ? PlayerType.Local : PlayerType.Remote,
                    PlayerIconIndex = participant.IconIndex,
                };

                list.Add(info);
            }

            playerIdMapper = mapper;
            return list;
        }
        #endregion

        #region Private Helpers
        private static string NormalizePlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return "Player";
            }

            return playerName.Trim();
        }
        #endregion
    }
}
