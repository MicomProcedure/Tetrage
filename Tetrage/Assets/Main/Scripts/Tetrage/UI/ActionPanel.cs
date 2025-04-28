using Tetrage.Actions;
using Tetrage.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Tetrage.UI
{

    public class ActionPanel : MonoBehaviour
    {
        [SerializeField] private Button openBtn;
        [SerializeField] private Button drawBtn;
        [SerializeField] private Button checkBtn;
        [SerializeField] private Button tetrageBtn;

        [SerializeField] private Player currentPlayer;

        void Start()
        {

            openBtn.onClick.AddListener(() => TryExecute(() => new OpenAction(currentPlayer)));
            //drawBtn.onClick.AddListener(() => TryExecute(() => new DrawAction(currentPlayer)));
            //checkBtn.onClick.AddListener(() => TryExecute(() => new CheckAction(currentPlayer)));
            //tetrageBtn.onClick.AddListener(() => TryExecute(() => new TetrageAction(currentPlayer)));
        }

        // Update is called once per frame
        void Update()
        {
            openBtn.interactable = new OpenAction(currentPlayer).Validate();
            //drawBtn.interactable = new DrawAction(currentPlayer).Validate();
            //checkBtn.interactable = new CheckAction(currentPlayer).Validate();
            //tetrageBtn.interactable = new TetrageAction(currentPlayer).Validate();
        }

        private void TryExecute(System.Func<GameAction> factory)
        {
            var action = factory(); // アクション生成
            currentPlayer.PerformAction(action);
        }
    }
}
