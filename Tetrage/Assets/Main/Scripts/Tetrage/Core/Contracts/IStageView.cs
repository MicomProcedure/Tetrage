using Tetrage.Models;
using UnityEngine;

namespace Tetrage.Core.Contracts
{
    public interface IStageView
    {
        public Transform StackRoot { get; }
        public Transform TrashRoot { get; }
        void SetStage(Stage stage);
    }
}