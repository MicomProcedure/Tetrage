using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Models;

namespace Tetrage.UI
{
    public class StageView : MonoBehaviour, IStageView
    {
        [SerializeField] private Transform _stackRoot;
        [SerializeField] private Transform _trashRoot;

        public Transform StackRoot => _stackRoot;
        public Transform TrashRoot => _trashRoot;

        public void SetStage(Stage stage)
        {
            throw new System.NotImplementedException();
        }
    }
}