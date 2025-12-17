using UnityEngine;

public class PanelOptions : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("No se encontró Animator en Panel_Options");
        }
    }
    public void ToggleUnicoBoton()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    void Open()
    {
        animator.SetTrigger("Open");
        isOpen = true;
    }

    void Close()
    {
        animator.SetTrigger("Close");
        isOpen = false;
    }
}
