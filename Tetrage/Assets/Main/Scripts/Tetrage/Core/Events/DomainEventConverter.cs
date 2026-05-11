using System;
using System.Collections.Generic;
using System.Linq;
using Tetrage.Core.Ids;
using Tetrage.Core.Enums;
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
                        throw new InvalidOperationException(
                            $"DomainEventConverter: ActorNumber {actorNumber} のPlayerIdマッピングが見つかりません（GameStarted）");
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.currentPlayerActorNumber} のPlayerIdマッピングが見つかりません（TurnStarted）");
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.previousPlayerActorNumber} のPlayerIdマッピングが見つかりません（TurnEnded）");
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
        public CardStateChangedEvent ToDomain(Tetrage.Network.Gameplay.CardStateChangedEvent dto)
        {
            var stateType = dto.stateCode switch
            {
                Tetrage.Network.Gameplay.CardStateCode.FaceUp => CardStateType.FaceUp,
                Tetrage.Network.Gameplay.CardStateCode.IsSuitVisible => CardStateType.IsSuitVisible,
                Tetrage.Network.Gameplay.CardStateCode.IsHighlighted => CardStateType.IsHighlighted,
                _ => CardStateType.Unknown,
            };

            var isFaceUp = dto.stateCode == Tetrage.Network.Gameplay.CardStateCode.FaceUp
                ? dto.stateValue
                : false;

            return new CardStateChangedEvent(
                sequence: dto.sequence,
                cardId: new CardId(dto.cardId),
                isFaceUp: isFaceUp,
                stateType: stateType,
                stateValue: dto.stateValue,
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
                        Debug.LogError($"DomainEventConverter: ActorNumber {actorNumber} のPlayerIdマッピングが見つかりません（GameEnded）");
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
                        Debug.LogError($"DomainEventConverter: ActorNumber {actorNumber} のPlayerIdマッピングが見つかりません（FinishingGame）");
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.userPlayerActorNumber} のPlayerIdマッピングが見つかりません（ScanPhaseStarted.userPlayer）");
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
                        Debug.LogError($"DomainEventConverter: ActorNumber {actorNumber} のPlayerIdマッピングが見つかりません（ScanPhaseStarted.players）");
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
        /// ScanTargetSelectedEvent (DTO) → ScanTargetSelectedEvent (Domain)
        /// </summary>
        public ScanTargetSelectedEvent ToDomain(Tetrage.Network.Gameplay.ScanTargetSelectedEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.actorPlayerId, out var actorPlayerId))
            {
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.actorPlayerId} のPlayerIdマッピングが見つかりません（ScanTargetSelected.actor）");
            }

            if (!_playerIdMapper.TryGetPlayerId(dto.selectedTargetActorNumber, out var selectedTargetPlayerId))
            {
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.selectedTargetActorNumber} のPlayerIdマッピングが見つかりません（ScanTargetSelected.target）");
            }

            return new ScanTargetSelectedEvent(
                sequence: dto.sequence,
                actorPlayerId: actorPlayerId,
                selectedTargetPlayerId: selectedTargetPlayerId,
                stateVersion: 0
            );
        }

        /// <summary>
        /// ScanResultEvent (DTO) → ScanResultReceivedEvent (Domain)
        /// </summary>
        public ScanResultReceivedEvent ToDomain(Tetrage.Network.Gameplay.ScanResultEvent dto)
        {
            if (!_playerIdMapper.TryGetPlayerId(dto.targetActorNumber, out var targetPlayerId))
            {
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.targetActorNumber} のPlayerIdマッピングが見つかりません（ScanResult.target）");
            }

            return new ScanResultReceivedEvent(
                sequence: dto.sequence,
                targetPlayerId: targetPlayerId,
                targetSuit: (Suit)dto.targetSuit,
                stateVersion: 0
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.actorPlayerId} のPlayerIdマッピングが見つかりません（ActionRequested）");
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: ActorNumber {dto.actorPlayerId} のPlayerIdマッピングが見つかりません（ActionResult）");
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
            var orderedIds = dto.orderedIds ?? Array.Empty<int>();

            switch (dto.idKind)
            {
                // ListKind に対応した ID 型へ境界で変換する
                case ListOrderIdKind.PlayerId:
                    return new ListOrderDeclaredEvent<PlayerId>(
                        sequence: dto.sequence,
                        idKind: dto.idKind,
                        listKey: dto.listKey,
                        orderedIds: orderedIds.Select(id => new PlayerId(id)).ToList(),
                        stateVersion: dto.stateVersion
                    );

                case ListOrderIdKind.CardId:
                    return new ListOrderDeclaredEvent<CardId>(
                        sequence: dto.sequence,
                        idKind: dto.idKind,
                        listKey: dto.listKey,
                        orderedIds: orderedIds.Select(id => new CardId(id)).ToList(),
                        stateVersion: dto.stateVersion
                    );

                case ListOrderIdKind.PileId:
                    return new ListOrderDeclaredEvent<PileId>(
                        sequence: dto.sequence,
                        idKind: dto.idKind,
                        listKey: dto.listKey,
                        orderedIds: orderedIds.Select(id => new PileId(id)).ToList(),
                        stateVersion: dto.stateVersion
                    );

                case ListOrderIdKind.DeckId:
                    return new ListOrderDeclaredEvent<DeckId>(
                        sequence: dto.sequence,
                        idKind: dto.idKind,
                        listKey: dto.listKey,
                        orderedIds: orderedIds.Select(id => new DeckId(id)).ToList(),
                        stateVersion: dto.stateVersion
                    );

                case ListOrderIdKind.Int:
                    return new ListOrderDeclaredEvent<int>(
                        sequence: dto.sequence,
                        idKind: dto.idKind,
                        listKey: dto.listKey,
                        orderedIds: orderedIds.ToList(),
                        stateVersion: dto.stateVersion
                    );

                default:
                    throw new InvalidOperationException($"DomainEventConverter: 未対応のListOrderIdKindです: {dto.idKind}");
            }
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: PlayerId {domainEvent.CurrentPlayerId} のActorNumberマッピングが見つかりません（TurnStarted→DTO）");
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
                throw new InvalidOperationException(
                    $"DomainEventConverter: PlayerId {domainEvent.PreviousPlayerId} のActorNumberマッピングが見つかりません（TurnEnded→DTO）");
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

