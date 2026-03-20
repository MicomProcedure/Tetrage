using UnityEngine;
using UnityEditor;
using Tetrage.Tests;
using Tetrage.Network.Gameplay;
using Tetrage.Core.Enums;

namespace Tetrage.Editor.Tests
{
    [CustomEditor(typeof(NetworkEventDebugger))]
    public class NetworkEventDebuggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            NetworkEventDebugger debugger = (NetworkEventDebugger)target;
            SerializedProperty eventCodeProp = serializedObject.FindProperty("_debugEventCode");
            SerializedProperty jsonPayloadProp = serializedObject.FindProperty("_debugJsonPayload");

            // enumValueIndexではなくintValueを使用（EventCodeは明示的な値を持つため）
            EventCode currentCode = (EventCode)eventCodeProp.intValue;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Template Generator", EditorStyles.boldLabel);

            if (GUILayout.Button($"Generate Template for {currentCode}", GUILayout.Height(25)))
            {
                string template = GenerateTemplateJson(currentCode);
                jsonPayloadProp.stringValue = template;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Send Network Event", GUILayout.Height(30)))
            {
                debugger.SendNetworkEvent();
            }
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Send Network Event ボタンはプレイモード中のみ使用できます。", MessageType.Info);
            }
        }

        private string GenerateTemplateJson(EventCode code)
        {
            switch (code)
            {
                case EventCode.GameStarted:
                    // deckId, suitOrder (byte[]), minNumber, maxNumber, playerActorNumbers
                    return @"{
    ""deckId"": 1,
    ""suitOrder"": [0, 1, 2, 3],
    ""minNumber"": 1,
    ""maxNumber"": 13,
    ""playerActorNumbers"": [1, 2, 3, 4]
}";
                case EventCode.TurnStarted:
                    // sequence, stateVersion, currentPlayerActorNumber
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""currentPlayerActorNumber"": 1
}";
                case EventCode.TurnEnded:
                    // sequence, stateVersion, previousPlayerActorNumber
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""previousPlayerActorNumber"": 1
}";
                case EventCode.ListOrderDeclared:
                    // sequence, stateVersion, idKind (ListOrderIdKind enum), listKey (ListOrderKey enum), orderedIds
                    // idKind: 0=Int, 1=PlayerId, 2=CardId, 3=PileId, 4=DeckId
                    // listKey: 1=TurnOrder, 2=UiSeats
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""idKind"": 1,
    ""listKey"": 1,
    ""orderedIds"": [1, 2, 3, 4]
}";
                case EventCode.CardMoved:
                    // sequence, stateVersion, cardId, fromPileId, toPileId
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""cardId"": 1,
    ""fromPileId"": 1,
    ""toPileId"": 2
}";
                case EventCode.CardVisibilityChanged:
                    // sequence, stateVersion, cardId, isVisible
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""cardId"": 1,
    ""isVisible"": true
}";
                case EventCode.StartScanPhase:
                    // sequence, stateVersion, userPlayerActorNumber, playerActorNumbers
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""userPlayerActorNumber"": 1,
    ""playerActorNumbers"": [1, 2, 3, 4]
}";
                case EventCode.EndScanPhase:
                    // sequence, stateVersion
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0
}";
                case EventCode.FinishingGame:
                    // sequence, stateVersion, winnerActorNumbers
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""winnerActorNumbers"": [1]
}";
                case EventCode.GameEnded:
                    // sequence, stateVersion, winnerActorNumbers
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""winnerActorNumbers"": [1]
}";
                case EventCode.PileShuffledWithSeed:
                    // sequence, stateVersion, pileId, seed
                    return @"{
    ""sequence"": 0,
    ""stateVersion"": 0,
    ""pileId"": 1,
    ""seed"": 12345
}";
                case EventCode.ActionRequested:
                    // sequence, clientSequence, actorPlayerId, actionType (ActionType enum), targetCardIds, actionStatusInt
                    // actionType: 0=Draw, 1=Open, 2=Reach, 3=Check, 4=Pass, 5=TetrageSolo, 6=TetrageMulti
                    return @"{
    ""sequence"": 0,
    ""clientSequence"": 0,
    ""actorPlayerId"": 1,
    ""actionType"": 0,
    ""targetCardIds"": [1],
    ""actionStatusInt"": 0
}";
                case EventCode.ActionResult:
                    // sequence, clientSequence, actorPlayerId, actionType (ActionType enum), accepted, reason, targetCardIds, actionStatusInt
                    // actionType: 0=Draw, 1=Open, 2=Reach, 3=Check, 4=Pass, 5=TetrageSolo, 6=TetrageMulti
                    return @"{
    ""sequence"": 0,
    ""clientSequence"": 0,
    ""actorPlayerId"": 1,
    ""actionType"": 0,
    ""accepted"": true,
    ""reason"": """",
    ""targetCardIds"": [1],
    ""actionStatusInt"": 0
}";
                default:
                    return "{}";
            }
        }
    }
}

