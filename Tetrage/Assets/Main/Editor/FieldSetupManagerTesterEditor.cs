#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

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
            EditorGUILayout.LabelField("テスト実行", EditorStyles.boldLabel);
            
            FieldSetupManagerTester tester = (FieldSetupManagerTester)target;
            
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
            var manager = tester.GetFieldSetupManager();
            if (manager != null)
            {
                EditorGUILayout.LabelField("セットアップ状態", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                try
                {
                    EditorGUILayout.LabelField($"プレイヤー数: {manager.Players.Count}");
                    EditorGUILayout.LabelField($"ステージ - Stack: {manager.Stage.Stack.Cards.Count}枚");
                    EditorGUILayout.LabelField($"ステージ - Trash: {manager.Stage.Trash.Cards.Count}枚");
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.LabelField($"エラー: {e.Message}", EditorStyles.helpBox);
                }
                
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("セットアップ状態: 未実行", EditorStyles.helpBox);
            }
        }
    }
}
#endif 