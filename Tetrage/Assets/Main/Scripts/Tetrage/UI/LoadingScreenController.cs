using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject loadingCanvasPrefab;

    private GameObject loadingCanvasInstance;

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
        }
    }

    public void DestroyLoadingCanvas()
    {
        if (loadingCanvasInstance != null)
        {
            Destroy(loadingCanvasInstance);
            loadingCanvasInstance = null;
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
