#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Tetrage.Tests
{
    /// <summary>
    /// CardDistributionTesterのCustom Editor
    /// Inspector上でカード配布とプレイヤー間転送を実行できるボタンUIを提供
    /// </summary>
    [CustomEditor(typeof(CardDistributionTester))]
    public class CardDistributionTesterEditor : Editor
    {
        private readonly string[] _pileTypeNames = { "Hands", "Tmp", "Target" };

        public override void OnInspectorGUI()
        {
            // デフォルトのInspector表示
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("カード配布テスト", EditorStyles.boldLabel);
            
            CardDistributionTester tester = (CardDistributionTester)target;
            
            // カード配布ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全プレイヤーに均等配布", GUILayout.Height(30)))
            {
                tester.DistributeCardsToAllPlayers();
            }
            if (GUILayout.Button("配布状況を表示", GUILayout.Height(30)))
            {
                tester.ShowDistributionStatus();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // プレイヤー間転送セクション
            EditorGUILayout.LabelField("プレイヤー間転送", EditorStyles.boldLabel);
            
            // 現在の設定表示
            SerializedProperty fromPlayerProp = serializedObject.FindProperty("fromPlayerIndex");
            SerializedProperty toPlayerProp = serializedObject.FindProperty("toPlayerIndex");
            SerializedProperty fromPileProp = serializedObject.FindProperty("fromPileType");
            SerializedProperty toPileProp = serializedObject.FindProperty("toPileType");
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"転送設定: プレイヤー{fromPlayerProp.intValue + 1}の{_pileTypeNames[fromPileProp.intValue]} → プレイヤー{toPlayerProp.intValue + 1}の{_pileTypeNames[toPileProp.intValue]}", EditorStyles.helpBox);
            EditorGUILayout.EndVertical();
            
            // 転送実行ボタン
            if (GUILayout.Button("プレイヤー間でカード転送", GUILayout.Height(25)))
            {
                tester.TransferCardBetweenPlayers();
            }
            
            EditorGUILayout.Space();
            
            // 便利機能セクション
            EditorGUILayout.LabelField("便利機能", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全カードをStackに戻す"))
            {
                tester.ReturnAllCardsToStack();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // FieldSetupManagerTesterとの連携状態表示
            var fieldSetupTester = tester.GetComponent<FieldSetupManagerTester>();
            if (fieldSetupTester != null)
            {
                EditorGUILayout.LabelField("連携状態", EditorStyles.boldLabel);
                
                var manager = fieldSetupTester.GetFieldSetupManager();
                if (manager != null)
                {
                    EditorGUILayout.BeginVertical("box");
                    
                    try
                    {
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
                    EditorGUILayout.HelpBox("FieldSetupManagerがセットアップされていません。\n先にFieldSetupManagerTesterでフィールドセットアップを実行してください。", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("FieldSetupManagerTesterが見つかりません。\n同じGameObjectにFieldSetupManagerTesterをアタッチしてください。", MessageType.Error);
            }
            
            EditorGUILayout.Space();
            
            // クイック設定ボタン
            EditorGUILayout.LabelField("クイック設定", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("手札配布設定"))
            {
                SetDistributionTarget(0); // Hands
            }
            if (GUILayout.Button("Tmp配布設定"))
            {
                SetDistributionTarget(1); // Tmp
            }
            if (GUILayout.Button("Target配布設定"))
            {
                SetDistributionTarget(2); // Target
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 配布先カードパイルを設定
        /// </summary>
        private void SetDistributionTarget(int pileIndex)
        {
            SerializedProperty targetPileProp = serializedObject.FindProperty("targetPileIndex");
            targetPileProp.intValue = pileIndex;
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"配布先を{_pileTypeNames[pileIndex]}に設定しました");
        }
    }
}
#endif 