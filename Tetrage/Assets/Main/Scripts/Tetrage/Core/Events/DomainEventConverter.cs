using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Ids;
using Tetrage.Network.Gameplay;
using UnityEngine;

namespace Tetrage.Core.Events
{
    /// <summary>
    /// NetworkDTOとDomainEventの双方向変換を行うコンバーター。
    /// Boundary層（NetworkEventApplier/Emitter）でのみ使用。
    /// </summary>
    public sealed class DomainEventConverter
    {
        private readonly IPlayerIdMapper _playerIdMapper;

        public DomainEventConverter(IPlayerIdMapper playerIdMapper)
        {
            _playerIdMapper = playerIdMapper;
        }

        #region NetworkDTO → DomainEvent

        /// <summary>
        /// GameStartedEvent (DTO) → GameStartedEvent (Domain)
        /// </summary>
        public GameStartedEvent ToDomain(Tetrage.Network.Gameplay.GameStartedEvent dto)
        {
            var playerIds = new List<PlayerId>();
            if (dto.playerActorNumbers != null)
            {
                foreach (var actorNumber in dto.playerActorNumbers)
                {
                    if (_playerIdMapper.TryGetPlayerId(actorNumber, out var playerId))
                    {
                        playerIds.Add(playerId);
                    }
                    else
                    {
                        Debug.LogWarning($"DomainEventConverter: ActorNumber {actorNumber} のマッピングが見つかりません");
                    }
                }
            }

            return new GameStartedEvent(
                sequence: 0, // DTOにはsequenceがない
                deckId: new DeckId(dto.deckId),
                suitOrder: dto.suitOrder,
                minNumber: dto.minNumber,
                maxNumber: dto.maxNumber,
                playerIds: playerIds,
                stateVersion: 0
            );
        }

        /// <summary>
        /// TurnStartedEvent (DTO) → TurnStartedEvent (Domain)
        /// </summary>
        public TurnStartedEvent ToDomain(Tetrage.Network.Gameplay.TurnStartedEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.currentPlayerActorNumber, out var playerId))
            {
                Debug.LogWarning($"DomainEventConverter: ActorNumber {dto.currentPlayerActorNumber} のマッピングが見つかりません");
                playerId = new PlayerId(-1); // フォールバック
            }

            return new TurnStartedEvent(
                sequence: dto.sequence,
                currentPlayerId: playerId,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// TurnEndedEvent (DTO) → TurnEndedEvent (Domain)
        /// </summary>
        public TurnEndedEvent ToDomain(Tetrage.Network.Gameplay.TurnEndedEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.previousPlayerActorNumber, out var playerId))
            {
                Debug.LogWarning($"DomainEventConverter: ActorNumber {dto.previousPlayerActorNumber} のマッピングが見つかりません");
                playerId = new PlayerId(-1); // フォールバック
            }

            return new TurnEndedEvent(
                sequence: dto.sequence,
                previousPlayerId: playerId,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// CardMovedEvent (DTO) → CardMovedEvent (Domain)
        /// </summary>
        public CardMovedEvent ToDomain(Tetrage.Network.Gameplay.CardMovedEvent dto)
        {
            return new CardMovedEvent(
                sequence: dto.sequence,
                cardId: new CardId(dto.cardId),
                fromPileId: new PileId(dto.fromPileId),
                toPileId: new PileId(dto.toPileId),
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// CardVisibilityChangedEvent (DTO) → CardVisibilityChangedEvent (Domain)
        /// </summary>
        public CardVisibilityChangedEvent ToDomain(Tetrage.Network.Gameplay.CardVisibilityChangedEvent dto)
        {
            return new CardVisibilityChangedEvent(
                sequence: dto.sequence,
                cardId: new CardId(dto.cardId),
                isVisible: dto.isVisible,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// GameEndedEvent (DTO) → GameEndedEvent (Domain)
        /// </summary>
        public GameEndedEvent ToDomain(Tetrage.Network.Gameplay.GameEndedEvent dto)
        {
            var winnerPlayerIds = new List<PlayerId>();
            if (dto.winnerActorNumbers != null)
            {
                foreach (var actorNumber in dto.winnerActorNumbers)
                {
                    if (actorNumber == -1) continue; // -1は未定/引き分け
                    if (_playerIdMapper.TryGetPlayerId(actorNumber, out var playerId))
                    {
                        winnerPlayerIds.Add(playerId);
                    }
                    else
                    {
                        Debug.LogWarning($"DomainEventConverter: ActorNumber {actorNumber} のマッピングが見つかりません");
                    }
                }
            }

            return new GameEndedEvent(
                sequence: dto.sequence,
                winnerPlayerIds: winnerPlayerIds,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// FinishingGameEvent (DTO) → FinishingGameEvent (Domain)
        /// </summary>
        public FinishingGameEvent ToDomain(Tetrage.Network.Gameplay.FinishingGameEvent dto)
        {
            var winnerPlayerIds = new List<PlayerId>();
            if (dto.winnerActorNumbers != null)
            {
                foreach (var actorNumber in dto.winnerActorNumbers)
                {
                    if (actorNumber == -1) continue; // -1は未定/引き分け
                    if (_playerIdMapper.TryGetPlayerId(actorNumber, out var playerId))
                    {
                        winnerPlayerIds.Add(playerId);
                    }
                    else
                    {
                        Debug.LogWarning($"DomainEventConverter: ActorNumber {actorNumber} のマッピングが見つかりません");
                    }
                }
            }

            return new FinishingGameEvent(
                sequence: dto.sequence,
                winnerPlayerIds: winnerPlayerIds,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// StartScanPhaseEvent (DTO) → ScanPhaseStartedEvent (Domain)
        /// </summary>
        public ScanPhaseStartedEvent ToDomain(Tetrage.Network.Gameplay.StartScanPhaseEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.userPlayerActorNumber, out var userPlayerId))
            {
                Debug.LogWarning($"DomainEventConverter: ActorNumber {dto.userPlayerActorNumber} のマッピングが見つかりません");
                userPlayerId = new PlayerId(-1);
            }

            var playerIds = new List<PlayerId>();
            if (dto.playerActorNumbers != null)
            {
                foreach (var actorNumber in dto.playerActorNumbers)
                {
                    if (_playerIdMapper.TryGetPlayerId(actorNumber, out var playerId))
                    {
                        playerIds.Add(playerId);
                    }
                    else
                    {
                        Debug.LogWarning($"DomainEventConverter: ActorNumber {actorNumber} のマッピングが見つかりません");
                    }
                }
            }

            return new ScanPhaseStartedEvent(
                sequence: dto.sequence,
                userPlayerId: userPlayerId,
                playerIds: playerIds,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// EndScanPhaseEvent (DTO) → ScanPhaseEndedEvent (Domain)
        /// </summary>
        public ScanPhaseEndedEvent ToDomain(Tetrage.Network.Gameplay.EndScanPhaseEvent dto)
        {
            return new ScanPhaseEndedEvent(
                sequence: dto.sequence,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// PileShuffledWithSeedEvent (DTO) → PileShuffledEvent (Domain)
        /// </summary>
        public PileShuffledEvent ToDomain(Tetrage.Network.Gameplay.PileShuffledWithSeedEvent dto)
        {
            return new PileShuffledEvent(
                sequence: dto.sequence,
                pileId: new PileId(dto.pileId),
                seed: dto.seed,
                stateVersion: dto.stateVersion
            );
        }

        /// <summary>
        /// ActionRequestedEvent (DTO) → ActionRequestedEvent (Domain)
        /// </summary>
        public ActionRequestedEvent ToDomain(Tetrage.Network.Gameplay.ActionRequestedEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.actorPlayerId, out var playerId))
            {
                Debug.LogWarning($"DomainEventConverter: ActorNumber {dto.actorPlayerId} のマッピングが見つかりません");
                playerId = new PlayerId(-1);
            }

            var cardIds = dto.targetCardIds?.Select(id => new CardId(id)).ToList() ?? new List<CardId>();

            return new ActionRequestedEvent(
                sequence: dto.sequence,
                clientSequence: dto.clientSequence,
                actorPlayerId: playerId,
                actionType: dto.actionType,
                targetCardIds: cardIds,
                actionStatusInt: dto.actionStatusInt,
                stateVersion: 0 // DTOにstateVersionがない
            );
        }

        /// <summary>
        /// ActionResultEvent (DTO) → ActionResultEvent (Domain)
        /// </summary>
        public ActionResultEvent ToDomain(Tetrage.Network.Gameplay.ActionResultEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.actorPlayerId, out var playerId))
            {
                Debug.LogWarning($"DomainEventConverter: ActorNumber {dto.actorPlayerId} のマッピングが見つかりません");
                playerId = new PlayerId(-1);
            }

            var cardIds = dto.targetCardIds?.Select(id => new CardId(id)).ToList() ?? new List<CardId>();

            return new ActionResultEvent(
                sequence: dto.sequence,
                clientSequence: dto.clientSequence,
                actorPlayerId: playerId,
                actionType: dto.actionType,
                accepted: dto.accepted,
                reason: dto.reason,
                targetCardIds: cardIds,
                actionStatusInt: dto.actionStatusInt,
                stateVersion: 0 // DTOにstateVersionがない
            );
        }

        /// <summary>
        /// ListOrderDeclaredEvent (DTO) → ListOrderDeclaredEvent (Domain)
        /// </summary>
        public ListOrderDeclaredEvent ToDomain(Tetrage.Network.Gameplay.ListOrderDeclaredEvent dto)
        {
            // orderedIdsはIDラッパーの実体値なので、そのまま渡す
            var orderedIds = dto.orderedIds?.ToList() ?? new List<int>();

            return new ListOrderDeclaredEvent(
                sequence: dto.sequence,
                idKind: dto.idKind,
                listKey: dto.listKey,
                orderedIds: orderedIds,
                stateVersion: dto.stateVersion
            );
        }

        #endregion

        #region DomainEvent → NetworkDTO（将来のEmitter用）

        /// <summary>
        /// TurnStartedEvent (Domain) → TurnStartedEvent (DTO)
        /// </summary>
        public Tetrage.Network.Gameplay.TurnStartedEvent ToDto(TurnStartedEvent domainEvent)
        {
            if (!_playerIdMapper.TryGetActorNumber(domainEvent.CurrentPlayerId, out var actorNumber))
            {
                Debug.LogWarning($"DomainEventConverter: PlayerId {domainEvent.CurrentPlayerId} のマッピングが見つかりません");
                actorNumber = -1;
            }

            return new Tetrage.Network.Gameplay.TurnStartedEvent
            {
                sequence = domainEvent.Sequence,
                stateVersion = domainEvent.StateVersion,
                currentPlayerActorNumber = actorNumber
            };
        }

        /// <summary>
        /// TurnEndedEvent (Domain) → TurnEndedEvent (DTO)
        /// </summary>
        public Tetrage.Network.Gameplay.TurnEndedEvent ToDto(TurnEndedEvent domainEvent)
        {
            if (!_playerIdMapper.TryGetActorNumber(domainEvent.PreviousPlayerId, out var actorNumber))
            {
                Debug.LogWarning($"DomainEventConverter: PlayerId {domainEvent.PreviousPlayerId} のマッピングが見つかりません");
                actorNumber = -1;
            }

            return new Tetrage.Network.Gameplay.TurnEndedEvent
            {
                sequence = domainEvent.Sequence,
                stateVersion = domainEvent.StateVersion,
                previousPlayerActorNumber = actorNumber
            };
        }

        // 他のDomainEvent → DTO変換は必要に応じて追加
        // 現時点ではEmitter側で直接DTO生成しているため、段階的に移行

        #endregion
    }
}

