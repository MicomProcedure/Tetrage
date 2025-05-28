using System.Collections.Generic;
using UnityEngine;

namespace Tetrage.Core.Contracts
{
    public interface IPositionConfig
    {
        List<Vector3> Position { get; }
        List<Quaternion> Rotation { get; } 
        bool ValidateConfig(int requiredCount);
    }
}
