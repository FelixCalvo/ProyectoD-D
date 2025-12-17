using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    public Light torchLight;

    [Header("Flicker Settings")]
    public float minIntensity = 1.2f;
    public float maxIntensity = 2.2f;
    public float flickerSpeed = 0.1f;

    public float minRange = 6f;
    public float maxRange = 8f;

    private float targetIntensity;
    private float targetRange;

    void Start()
    {
        if (torchLight == null)
            torchLight = GetComponent<Light>();

        PickNewTargets();
    }

    void Update()
    {
        torchLight.intensity = Mathf.Lerp(
            torchLight.intensity,
            targetIntensity,
            Time.deltaTime * 10f
        );

        torchLight.range = Mathf.Lerp(
            torchLight.range,
            targetRange,
            Time.deltaTime * 10f
        );

        if (Mathf.Abs(torchLight.intensity - targetIntensity) < 0.05f)
        {
            PickNewTargets();
        }
    }

    void PickNewTargets()
    {
        targetIntensity = Random.Range(minIntensity, maxIntensity);
        targetRange = Random.Range(minRange, maxRange);
    }
}
