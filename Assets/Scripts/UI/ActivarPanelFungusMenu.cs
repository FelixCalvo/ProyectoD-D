using UnityEngine;

public class ActivarPanelFungusMenu : MonoBehaviour
{


//Necesitamos activar el panel del menu de fungus al iniciar la escena. 
//Es una copia del original ya que no me dejaba alterar la altura del menu de fungus.
    [SerializeField] private GameObject panelFungusMenu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panelFungusMenu.SetActive(true);
    }
}
