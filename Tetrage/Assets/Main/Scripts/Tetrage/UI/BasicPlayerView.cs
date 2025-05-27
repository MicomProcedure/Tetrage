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

        public Transform HandsRoot => _handsRoot;
        public Transform TargetRoot => _targetRoot;
        public Transform TmpRoot => _tmpRoot;
        public Transform PlayerUIRoot => _playerUIRoot;

        public void SetPlayer(IPlayer player)
        {
            throw new System.NotImplementedException();
        }
    }
}