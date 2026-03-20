using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Tetrage.Core.Contracts;
using Tetrage.Managers;
using UnityEngine;

namespace Tetrage.Network
{
    /// <summary>
    /// PhotonイベントをPlayerSessionへ同期するアダプタ。
    /// </summary>
    public sealed class PhotonSessionSync : MonoBehaviourPunCallbacks
    {
        #region Fields
        private readonly Dictionary<int, int> _actorToSessionId = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _sessionToActorNumber = new Dictionary<int, int>();
        #endregion

        #region Public API
        public IReadOnlyDictionary<int, int> SessionToActorNumberMap => _sessionToActorNumber;
        #endregion

        #region Photon Callbacks
        public override void OnJoinedRoom()
        {
            RebuildFromRoomSnapshot();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (newPlayer == null)
            {
                return;
            }

            AddOrUpdateFromPhotonPlayer(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (otherPlayer == null)
            {
                return;
            }

            if (!_actorToSessionId.TryGetValue(otherPlayer.ActorNumber, out var sessionId))
            {
                return;
            }

            var session = ResolveSession();
            if (session == null)
            {
                return;
            }

            session.RemoveParticipant(sessionId);
            _actorToSessionId.Remove(otherPlayer.ActorNumber);
            _sessionToActorNumber.Remove(sessionId);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer == null)
            {
                return;
            }

            AddOrUpdateFromPhotonPlayer(targetPlayer);
        }

        public override void OnLeftRoom()
        {
            _actorToSessionId.Clear();
            _sessionToActorNumber.Clear();
        }
        #endregion

        #region Private Helpers
        private void RebuildFromRoomSnapshot()
        {
            var session = ResolveSession();
            if (session == null)
            {
                return;
            }

            session.Clear(false);
            _actorToSessionId.Clear();
            _sessionToActorNumber.Clear();

            var players = PhotonNetwork.PlayerList.OrderBy(x => x.ActorNumber).ToArray();
            for (int i = 0; i < players.Length; i++)
            {
                AddOrUpdateFromPhotonPlayer(players[i]);
            }
        }

        private void AddOrUpdateFromPhotonPlayer(Player photonPlayer)
        {
            var session = ResolveSession();
            if (session == null)
            {
                return;
            }

            var playerName = string.IsNullOrWhiteSpace(photonPlayer.NickName)
                ? $"Player_{photonPlayer.ActorNumber}"
                : photonPlayer.NickName;
            var iconIndex = ExtractIconIndex(photonPlayer);

            if (_actorToSessionId.TryGetValue(photonPlayer.ActorNumber, out var existingSessionId))
            {
                session.UpdateParticipant(existingSessionId, playerName, iconIndex, photonPlayer.IsLocal);
                return;
            }

            var sessionId = session.AddParticipant(playerName, iconIndex, photonPlayer.IsLocal);
            _actorToSessionId[photonPlayer.ActorNumber] = sessionId;
            _sessionToActorNumber[sessionId] = photonPlayer.ActorNumber;
        }

        private static int ExtractIconIndex(Player player)
        {
            if (player.CustomProperties == null || !player.CustomProperties.ContainsKey("IconIndex"))
            {
                return 0;
            }

            try
            {
                return (int)player.CustomProperties["IconIndex"];
            }
            catch
            {
                return 0;
            }
        }

        private static IPlayerSession ResolveSession()
        {
            if (ApplicationManager.Instance == null)
            {
                return null;
            }

            return ApplicationManager.Instance.PlayerSession;
        }
        #endregion
    }
}
