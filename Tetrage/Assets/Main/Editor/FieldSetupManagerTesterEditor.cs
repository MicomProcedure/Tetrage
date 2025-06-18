#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Tetrage.Components;

namespace Tetrage.Tests
{
    /// <summary>
    /// FieldSetupManagerTesterのCustom Editor
    /// Inspectorに使いやすいボタンUIを追加します
    /// </summary>
    [CustomEditor(typeof(FieldSetupManagerTester))]
    public class FieldSetupManagerTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのInspector表示
            DrawDefaultInspector();

            EditorGUILayout.Space();

            FieldSetupManagerTester tester = (FieldSetupManagerTester)target;

            // 現在の設定状況を表示
            DrawCurrentSettingsInfo(tester);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("テスト実行", EditorStyles.boldLabel);

            // 設定状況確認ボタン
            if (GUILayout.Button("設定状況を表示", GUILayout.Height(25)))
            {
                tester.ShowCurrentSettings();
            }

            // フィールドセットアップボタン
            if (GUILayout.Button("フィールドセットアップを実行", GUILayout.Height(30)))
            {
                tester.SetupField();
            }

            EditorGUILayout.Space();

            // フィールドクリアボタン
            if (GUILayout.Button("フィールドをクリア", GUILayout.Height(25)))
            {
                tester.ClearField();
            }

            EditorGUILayout.Space();

            // セットアップ状態の表示
            DrawSetupStatusInfo(tester);
        }

        /// <summary>
        /// 現在の設定状況を表示
        /// </summary>
        private void DrawCurrentSettingsInfo(FieldSetupManagerTester tester)
        {
            EditorGUILayout.LabelField("設定ソース", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            // FieldSetupComponentの検索
            var fieldSetupComponentProp = serializedObject.FindProperty("fieldSetupComponent");
            var forceUseInspectorProp = serializedObject.FindProperty("forceUseInspectorSettings");

            FieldSetupComponent targetComponent = null;

            // Inspector設定のFieldSetupComponent
            if (fieldSetupComponentProp.objectReferenceValue != null)
            {
                targetComponent = fieldSetupComponentProp.objectReferenceValue as FieldSetupComponent;
                EditorGUILayout.LabelField($"Inspector設定: {targetComponent.name}", EditorStyles.helpBox);
            }
            else
            {
                // シーン内検索
                var foundComponent = FindObjectOfType<FieldSetupComponent>();
                if (foundComponent != null)
                {
                    targetComponent = foundComponent;
                    EditorGUILayout.LabelField($"シーン内発見: {foundComponent.name}", EditorStyles.helpBox);
                }
                else
                {
                    EditorGUILayout.LabelField("FieldSetupComponent: 見つかりません", EditorStyles.helpBox);
                }
            }

            // 実際に使用される設定ソースの表示
            if (targetComponent != null && !forceUseInspectorProp.boolValue)
            {
                EditorGUILayout.LabelField("使用予定: FieldSetupComponent", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"対象: {targetComponent.name}");
            }
            else
            {
                EditorGUILayout.LabelField("使用予定: Inspector設定", EditorStyles.boldLabel);
                if (forceUseInspectorProp.boolValue)
                {
                    EditorGUILayout.LabelField("(強制使用モード)", EditorStyles.helpBox);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// セットアップ状態の表示
        /// </summary>
        private void DrawSetupStatusInfo(FieldSetupManagerTester tester)
        {
            EditorGUILayout.LabelField("セットアップ状態", EditorStyles.boldLabel);

            var manager = tester.GetFieldSetupManager();
            if (manager != null)
            {
                EditorGUILayout.BeginVertical("box");

                try
                {
                    // 使用された設定ソースを表示
                    string settingSource = tester.IsUsingFieldSetupComponent() ? "FieldSetupComponent" : "Inspector設定";
                    EditorGUILayout.LabelField($"設定ソース: {settingSource}", EditorStyles.boldLabel);

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField($"プレイヤー数: {manager.Players.Count}");
                    EditorGUILayout.LabelField($"Stack: {manager.Stage.Stack.Cards.Count}枚");
                    EditorGUILayout.LabelField($"Trash: {manager.Stage.Trash.Cards.Count}枚");

                    EditorGUILayout.Space();

                    // プレイヤー別カード枚数表示
                    for (int i = 0; i < manager.Players.Count; i++)
                    {
                        var player = manager.Players[i];
                        EditorGUILayout.LabelField($"プレイヤー{i + 1} ({player.UserId}):");
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"Hands: {player.Hands.Cards.Count}枚, Tmp: {player.Tmp.Cards.Count}枚, Target: {player.Target.Cards.Count}枚");
                        EditorGUI.indentLevel--;
                    }
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.LabelField($"エラー: {e.Message}", EditorStyles.helpBox);
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("FieldSetupManagerがセットアップされていません。\n「フィールドセットアップを実行」ボタンでセットアップしてください。", MessageType.Info);
            }

            // FieldSetupComponentとの連携状態を表示
            var fieldSetupComponent = serializedObject.FindProperty("fieldSetupComponent").objectReferenceValue as FieldSetupComponent;
            if (fieldSetupComponent == null)
            {
                fieldSetupComponent = FindObjectOfType<FieldSetupComponent>();
            }

            if (fieldSetupComponent != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("FieldSetupComponent連携", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField($"対象: {fieldSetupComponent.name}");

                if (fieldSetupComponent.FieldSetupManager != null)
                {
                    EditorGUILayout.LabelField("状態: セットアップ済み", EditorStyles.helpBox);
                }
                else
                {
                    EditorGUILayout.LabelField("状態: 未セットアップ", EditorStyles.helpBox);
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif