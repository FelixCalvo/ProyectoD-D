using UnityEngine;

public class Cocinero : MonoBehaviour
{
    void Update()
    {
        // No permitir clicks si hay diálogo activo
        if (DialogueBlocker.Instance != null && DialogueBlocker.Instance.IsDialogueActive)
        {
            return;
        }
        
        // Detectar click atravesando paredes transparentes
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] allHits = Physics.RaycastAll(ray, 1000f);
            
            // Ordenar por distancia
            System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
            
            foreach (RaycastHit hit in allHits)
            {
                // Ignorar paredes transparentes
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ObjetosTransparentes"))
                    continue;
                
                // Si el click es en este NPC
                if (hit.collider.gameObject == gameObject)
                {
                    print("Hola soy un Cocinero");
                    Fungus.Flowchart.BroadcastFungusMessage("CocineroClicked");
                    return;
                }
                
                // Si encontramos otro objeto primero, salir
                break;
            }
        }
    }
}
