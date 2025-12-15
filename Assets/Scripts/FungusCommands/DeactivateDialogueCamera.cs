using Fungus;
using UnityEngine;

namespace Fungus
{
    /// <summary>
    /// Comando Fungus que desactiva la cámara de diálogo activa.
    /// Usar al final de un bloque de diálogo para volver a la MainCamera.
    /// </summary>
    [CommandInfo("Camera",
                 "Deactivate Dialogue Camera",
                 "Desactiva la cámara de diálogo y vuelve a la cámara por defecto")]
    public class DeactivateDialogueCamera : Command
    {
        public override void OnEnter()
        {
            if (DialogueCameraManager.Instance != null)
            {
                DialogueCameraManager.Instance.DeactivateDialogueCamera();
            }

            Continue();
        }

        public override string GetSummary()
        {
            return "Volver a cámara principal";
        }
    }
}
