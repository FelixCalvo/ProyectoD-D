using UnityEngine;

/// <summary>
/// Tester simple - Presiona T para probar
/// </summary>
public class LocalizationTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Testing: " + LocalizationManager.Instance.GetText("WELCOME_MESSAGE"));
        }
    }
}
