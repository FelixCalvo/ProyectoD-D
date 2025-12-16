using UnityEngine;
using Fungus;

/// <summary>
/// Reemplazo de Clickable2D que funciona a través de paredes transparentes.
/// Añadir este componente en lugar de Clickable2D a NPCs.
/// </summary>
public class ClickableThroughWalls : MonoBehaviour
{
    [Tooltip("Is object clicking enabled")]
    [SerializeField] protected bool clickEnabled = true;

    [Tooltip("Mouse texture to use when hovering mouse over object")]
    [SerializeField] protected Texture2D hoverCursor;

    private bool isHovering = false;

    void Update()
    {
        if (!clickEnabled)
            return;

        // Verificar si el mouse está sobre este objeto (ignorando transparentes)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] allHits = Physics.RaycastAll(ray, 1000f);
        
        // Ordenar por distancia
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
        
        bool mouseOver = false;
        foreach (RaycastHit hit in allHits)
        {
            // Ignorar objetos transparentes
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                continue;
            
            // Verificar si el hit es este objeto
            if (hit.collider.gameObject == gameObject)
            {
                mouseOver = true;
                break;
            }
            
            // Si encontramos otro objeto primero, salir
            break;
        }

        // Manejar hover
        if (mouseOver && !isHovering)
        {
            isHovering = true;
            if (hoverCursor != null)
            {
                Cursor.SetCursor(hoverCursor, Vector2.zero, CursorMode.Auto);
            }
        }
        else if (!mouseOver && isHovering)
        {
            isHovering = false;
            SetMouseCursor.ResetMouseCursor();
        }

        // Detectar click
        if (mouseOver && Input.GetMouseButtonDown(0))
        {
            DoPointerClick();
        }
    }

    protected virtual void DoPointerClick()
    {
        var eventDispatcher = FungusManager.Instance.EventDispatcher;
        
        // Crear un Clickable2D temporal para mantener compatibilidad
        var tempClickable = gameObject.AddComponent<Clickable2D>();
        eventDispatcher.Raise(new ObjectClicked.ObjectClickedEvent(tempClickable));
        Destroy(tempClickable);
    }

    void OnDisable()
    {
        if (isHovering)
        {
            SetMouseCursor.ResetMouseCursor();
        }
    }

    /// <summary>
    /// Is object clicking enabled.
    /// </summary>
    public bool ClickEnabled 
    { 
        get { return clickEnabled; }
        set { clickEnabled = value; } 
    }
}
