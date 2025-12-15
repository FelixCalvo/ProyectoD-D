using Fungus;
using UnityEngine;
using Unity.Cinemachine;

namespace Fungus
{
    /// <summary>
    /// Comando Fungus que activa una cámara Cinemachine específica.
    /// Usar al inicio de un bloque de diálogo.
    /// </summary>
    [CommandInfo("Camera",
                 "Activate Dialogue Camera",
                 "Activa una cámara virtual específica para el diálogo")]
    public class ActivateDialogueCamera : Command
    {
        [Tooltip("La cámara virtual de Cinemachine a activar")]
        [SerializeField] protected CinemachineCamera targetCamera;

        public override void OnEnter()
        {
            if (DialogueCameraManager.Instance != null && targetCamera != null)
            {
                DialogueCameraManager.Instance.ActivateCamera(targetCamera);
            }

            Continue();
        }

        public override string GetSummary()
        {
            if (targetCamera == null)
                return "Error: No se asignó cámara";
            
            return $"Activar: {targetCamera.name}";
        }
    }
}
