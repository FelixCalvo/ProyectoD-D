using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
  private NetworkCharacterController _cc;

  private void Awake()
  {
    _cc = GetComponent<NetworkCharacterController>();
  }

  public override void Spawned()
  {
    base.Spawned();
    
    Debug.Log($"🎮 [{gameObject.name}] Spawned en cliente. IsLocal: {Object.HasInputAuthority} | StateAuthority: {Object.HasStateAuthority}");
    
    // Forzar visibilidad de todos los renderers
    var renderers = GetComponentsInChildren<Renderer>(true);
    foreach (var renderer in renderers)
    {
      renderer.enabled = true;
      renderer.gameObject.SetActive(true);
    }
    
    // Desactivar LOD si existe (puede causar invisibilidad en clientes)
    var lodGroups = GetComponentsInChildren<LODGroup>(true);
    foreach (var lod in lodGroups)
    {
      lod.enabled = false;
    }
  }

  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      data.direction.Normalize();
      _cc.Move(5*data.direction*Runner.DeltaTime);
    }
  }
}