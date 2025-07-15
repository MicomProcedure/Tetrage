using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    [SerializeField] private GameObject loadingCanvasPrefab;

    private GameObject loadingCanvasInstance;
    private GameObject loadingPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CreateLoadingCanvas()
    {
        if (loadingCanvasInstance == null && loadingCanvasPrefab != null)
        {
            loadingCanvasInstance = Instantiate(loadingCanvasPrefab);
            // Panelを自動取得（"LoadingPanel"という名前のGameObjectを想定）
            loadingPanel = loadingCanvasInstance.transform.Find("LoadingPanel")?.gameObject ?? loadingCanvasInstance;
        }
    }

    public void DestroyLoadingCanvas()
    {
        if (loadingCanvasInstance != null)
        {
            Destroy(loadingCanvasInstance);
            loadingCanvasInstance = null;
            loadingPanel = null;
        }
    }

    public void Show()
    {
        loadingPanel?.SetActive(true);
    }

    public void Hide()
    {
        loadingPanel?.SetActive(false);
    }
}
