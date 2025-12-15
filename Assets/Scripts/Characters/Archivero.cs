using UnityEngine;
using Fungus;   

public class Archivero : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        print("Hola soy un archivero");
        Fungus.Flowchart.BroadcastFungusMessage("ArchiveroClicked");
    }

    public void PruebaCallMethodFungus()
    {
        print("Metodo llamado desde Fungus");
        
    }
}
