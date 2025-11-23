using UnityEngine;
using UnityEditor;
using Tetrage.Tests;
using Tetrage.Core.Constants;

namespace Tetrage.Editor.Tests
{
    [CustomEditor(typeof(GameScenePlayerDebugSettings))]
    public class GameScenePlayerDebugSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GameScenePlayerDebugSettings settings = (GameScenePlayerDebugSettings)target;
            SerializedProperty debugPlayerInfosProp = serializedObject.FindProperty("_debugPlayerInfos");

            // GameSceneDebugEntrySimpleからプレイヤー数を取得を試みる
            int playerCount = GetPlayerCountFromScene(settings);
            if (playerCount == 0)
            {
                // 取得できない場合はリストのサイズを基準にチェック
                playerCount = debugPlayerInfosProp.arraySize;
                if (playerCount == 0)
                {
                    playerCount = SettingConsts.MIN_PLAYER_COUNT; // デフォルト値として最小プレイヤー数を使用
                }
            }

            // IsUserPlayerがtrueのプレイヤー数をカウント
            int userPlayerCount = 0;
            var playerIdSet = new System.Collections.Generic.HashSet<int>();
            var duplicatePlayerIds = new System.Collections.Generic.List<int>();

            for (int i = 0; i < playerCount && i < debugPlayerInfosProp.arraySize; i++)
            {
                SerializedProperty element = debugPlayerInfosProp.GetArrayElementAtIndex(i);
                SerializedProperty isUserPlayerProp = element.FindPropertyRelative("IsUserPlayer");
                SerializedProperty playerIdProp = element.FindPropertyRelative("PlayerId");

                if (isUserPlayerProp.boolValue)
                {
                    userPlayerCount++;
                }

                // PlayerIdの重複チェック（0以外の値のみ）
                int playerIdValue = playerIdProp.intValue;
                if (playerIdValue > 0)
                {
                    if (playerIdSet.Contains(playerIdValue))
                    {
                        if (!duplicatePlayerIds.Contains(playerIdValue))
                        {
                            duplicatePlayerIds.Add(playerIdValue);
                        }
                    }
                    else
                    {
                        playerIdSet.Add(playerIdValue);
                    }
                }
            }

            // 警告表示
            if (userPlayerCount > 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    $"警告: {userPlayerCount}人のプレイヤーがUserPlayerに設定されています。\n" +
                    "内部では最初の一人のみがUserPlayerとして使用されます。",
                    MessageType.Warning
                );
            }
            else if (userPlayerCount == 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "UserPlayerが設定されていません。\n" +
                    "GameSceneDebugEntrySimpleの_localPlayerIndexが使用されます。",
                    MessageType.Info
                );
            }

            if (duplicatePlayerIds.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    $"警告: PlayerIdが重複しています: {string.Join(", ", duplicatePlayerIds)}\n" +
                    "重複したPlayerIdは予期しない動作を引き起こす可能性があります。",
                    MessageType.Warning
                );
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// シーン内のGameSceneDebugEntrySimpleからプレイヤー数を取得
        /// </summary>
        private int GetPlayerCountFromScene(GameScenePlayerDebugSettings settings)
        {
            // 同じGameObjectまたは親子関係にあるGameSceneDebugEntrySimpleを探す
            var debugEntry = settings.GetComponent<GameSceneDebugEntrySimple>();
            if (debugEntry == null)
            {
                debugEntry = settings.GetComponentInParent<GameSceneDebugEntrySimple>();
            }
            if (debugEntry == null)
            {
                // シーン全体から探す
                debugEntry = Object.FindFirstObjectByType<GameSceneDebugEntrySimple>();
            }

            if (debugEntry != null)
            {
                // リフレクションで_playerCountを取得
                var field = typeof(GameSceneDebugEntrySimple).GetField("_playerCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (int)field.GetValue(debugEntry);
                }
            }

            return 0;
        }
    }
}

