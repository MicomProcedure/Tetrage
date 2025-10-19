#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tetrage.Data.Editor
{
    [CustomEditor(typeof(CardImageMapper))]
    public class CardImageMapperEditor : UnityEditor.Editor
    {
        private SerializedProperty cardSpritesProperty;
        private SerializedProperty cardBackSpriteProperty;

        private void OnEnable()
        {
            cardSpritesProperty = serializedObject.FindProperty("cardSprites");
            cardBackSpriteProperty = serializedObject.FindProperty("cardBackSprite");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Card Back", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cardBackSpriteProperty);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Card Sprites", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate All Card Data (52 cards)", GUILayout.Height(30)))
            {
                var mapper = target as CardImageMapper;
                mapper.GenerateAllCardData();
            }

            EditorGUILayout.Space();

            // カードスプライトリストをグループ化して表示
            if (cardSpritesProperty.arraySize > 0)
            {
                DrawCardSpritesList();
            }
            else
            {
                EditorGUILayout.HelpBox("Click 'Generate All Card Data' to create 52 card entries.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCardSpritesList()
        {
            var mapper = target as CardImageMapper;
            var suits = System.Enum.GetValues(typeof(Core.Enums.Suit));

            foreach (Core.Enums.Suit suit in suits)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(suit.ToString(), EditorStyles.boldLabel);

                for (int i = 0; i < cardSpritesProperty.arraySize; i++)
                {
                    var element = cardSpritesProperty.GetArrayElementAtIndex(i);
                    var suitProp = element.FindPropertyRelative("suit");
                    var numberProp = element.FindPropertyRelative("number");
                    var spriteProp = element.FindPropertyRelative("sprite");

                    if ((Core.Enums.Suit)suitProp.enumValueIndex == suit)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{GetNumberName(numberProp.intValue)}", GUILayout.Width(30));
                        EditorGUILayout.PropertyField(spriteProp, GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private string GetNumberName(int number)
        {
            return number switch
            {
                1 => "A",
                11 => "J",
                12 => "Q",
                13 => "K",
                _ => number.ToString()
            };
        }
    }
}
#endif