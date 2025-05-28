using UnityEngine;
using Tetrage.Core.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace Tetrage.Components
{
    public class PositionMarker : MonoBehaviour, IPositionConfig
    {

        [SerializeField]
        [Tooltip("The positions of the marker")]
        private List<Transform> positions;

        public List<Vector3> Position => positions.Select(p => p.position).ToList();

        public List<Quaternion> Rotation => positions.Select(p => p.rotation).ToList();

        public bool ValidateConfig(int requiredCount)
        {
            return Position.Count == requiredCount && Rotation.Count == requiredCount;
        }

    }
}
