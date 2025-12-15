using Fungus;
using UnityEngine;

namespace Fungus
{
    [CommandInfo(
        "Narrative",
        "Log Selected Menu",
        "Guarda en el diario la opción de menú que el jugador acaba de seleccionar. Colocar al inicio del bloque del menú."
    )]
    public class LogSelectedMenu : Command
    {
        public override void OnEnter()
        {
            // Obtener el bloque actual
            Block currentBlock = ParentBlock;

            // Intentar obtener el texto del menú que llevó a este bloque
            string menuText = MenuJournalTracker.Instance.GetAndClearMenuText(currentBlock);

            if (!string.IsNullOrEmpty(menuText) && AdventureJournal.Instance != null)
            {
                // Guardar en el diario con el prefijo → para indicar que es una elección del jugador
                AdventureJournal.Instance.AddEntry("Jugador", $"→ {menuText}");
            }

            Continue();
        }

        public override string GetSummary()
        {
            return "Guarda la opción de menú seleccionada en el diario";
        }

        public override Color GetButtonColor()
        {
            return new Color32(184, 210, 235, 255);
        }
    }
}
