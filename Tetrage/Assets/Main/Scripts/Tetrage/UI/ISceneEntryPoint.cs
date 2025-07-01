using UnityEngine;
using MackySoft.Navigathena;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using MackySoft.Navigathena.SceneManagement;

public interface ISceneEntryPoint
{
    UniTask OnInitialize(
        ISceneDataReader dataReader,
        IProgress<IProgressDataStore> progress,
        CancellationToken cancellationToken
    );
}
