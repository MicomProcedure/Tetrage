using UnityEngine;
using Cysharp.Threading.Tasks;

public class DialogTest : MonoBehaviour
{
    async void Start()
    {
        await DialogManager.Instance.ShowDialog(
            "Test Dialog",
            "This dialog is for testing. Please press OK or Cancel.",
            onOk: () => Debug.Log("OK button pressed"),
            onCancel: () => Debug.Log("Cancel button pressed")
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
