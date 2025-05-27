using UnityEngine;
using Tetrage.Core.Contracts;
using Tetrage.Models;

namespace Tetrage.UI
{
    public class BasicPlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] private Transform _handsRoot;
        [SerializeField] private Transform _targetRoot;
        [SerializeField] private Transform _tmpRoot;
        [SerializeField] private Transform _playerUIRoot;

        public void SetPlayer(IPlayer player)
        {
            throw new System.NotImplementedException();
        }
    }
}