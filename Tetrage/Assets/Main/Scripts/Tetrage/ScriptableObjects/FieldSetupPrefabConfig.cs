using UnityEngine;
using System.Collections.Generic;
using Tetrage.UI;
using Tetrage.Core.DTO;
using Tetrage.Components;
using Tetrage.Core.Contracts;
using Tetrage.Core.Enums;

[CreateAssetMenu(fileName = "FieldSetupPrefabConfig", menuName = "Scriptable Objects/FieldSetupPrefabConfig")]
public class FieldSetupPrefabConfig : ScriptableObject
{
    [Header("Position Configs")]
    [SerializeField] private PositionConfig playerPositionConfigPrefab;
    [SerializeField] private PositionConfig stagePositionConfigPrefab;
    public IPositionConfig PlayerPositionConfig => playerPositionConfigPrefab;
    public IPositionConfig StagePositionConfig => stagePositionConfigPrefab;

    [Header("Player Views")]
    public BasicPlayerView LocalPlayerViewPrefab;
    public BasicPlayerView RemotePlayerViewPrefab;
    public BasicPlayerView BotPlayerViewPrefab;
    [Header("Card Views")]
    public CardView CardViewPrefab;

    [Header("Stage Views")]
    public StageView StageViewPrefab;

    [Header("Card Pile Views")]
    public BasicCardPileView BasicCardPileViewPrefab;
    public BasicCardPileView HandsCardPileViewPrefab;
    public BasicCardPileView TmpCardPileViewPrefab;
    public BasicCardPileView StackCardPileViewPrefab;
    public BasicCardPileView TrashCardPileViewPrefab;

}
