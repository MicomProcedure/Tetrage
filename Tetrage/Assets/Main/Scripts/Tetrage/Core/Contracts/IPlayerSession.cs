using System;
using System.Collections.Generic;
using Tetrage.Core.DTO;
using Tetrage.Network.Gameplay;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// プレイヤーのセッション状態を管理するインターフェース。
    /// Photonなどのネットワーク実装を知らずに扱えることを目的とする。
    /// </summary>
    public interface IPlayerSession
    {
        #region Properties
        IReadOnlyList<SessionPlayerData> Participants { get; }
        SessionPlayerData LocalPlayer { get; }
        int ParticipantCount { get; }
        #endregion

        #region Session Operations
        void RegisterLocalPlayer(string name, int iconIndex);
        int AddParticipant(string name, int iconIndex, bool isLocal = false);
        bool RemoveParticipant(int sessionId);
        void UpdateParticipant(int sessionId, string name = null, int? iconIndex = null, bool? isLocal = null);
        void Clear(bool preserveLocalPlayer = true);
        List<PlayerInfo> BuildPlayerInfos(out IPlayerIdMapper playerIdMapper, IReadOnlyDictionary<int, int> sessionIdToActorNumberMap = null);
        #endregion

        #region Events
        event Action<SessionPlayerData> OnParticipantAdded;
        event Action<SessionPlayerData> OnParticipantUpdated;
        event Action<int> OnParticipantRemoved;
        event Action OnSessionCleared;
        #endregion
    }
}
