using UnityEngine;
using MackySoft.Navigathena.SceneManagement;
using MackySoft.Navigathena;
using Tetrage.UI;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class TitleEntryPoint : SceneEntryPointBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override UniTask OnInitialize(
        ISceneDataReader dataReader,
        IProgress<IProgressDataStore> progress,
        CancellationToken cancellationToken
    )
    {
        // 必要な初期化処理
        return UniTask.CompletedTask;
    }
}
