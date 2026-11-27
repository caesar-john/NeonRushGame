using UnityEngine;

// Animasi ringan pada visual 3D hazard neon (rotasi plasma core, kedip beacon, denyut laser).
public class NeonHazardVisualFX : MonoBehaviour
{
    public Transform spinner;
    public float spinSpeed = 120f;
    public Transform hoverTarget;
    public float hoverAmplitude = 0.14f;
    public float hoverSpeed = 3.2f;
    public Renderer[] pulseRenderers;
    public Color pulseBaseColor = new Color(1f, 0.15f, 0.55f);
    public float pulseMinIntensity = 1.6f;
    public float pulseMaxIntensity = 3.4f;
    public float pulseSpeed = 5.5f;

    private Vector3 hoverStartLocal;
    private float phaseOffset;
    private MaterialPropertyBlock propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        if (hoverTarget != null) hoverStartLocal = hoverTarget.localPosition;
        propBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float t = Time.time;

        if (spinner != null)
            spinner.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        if (hoverTarget != null)
        {
            Vector3 p = hoverStartLocal;
            p.y += Mathf.Sin(t * hoverSpeed + phaseOffset) * hoverAmplitude;
            hoverTarget.localPosition = p;
        }

        if (pulseRenderers != null && pulseRenderers.Length > 0)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed + phaseOffset);
            float intensity = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, wave);
            Color hdr = new Color(pulseBaseColor.r * intensity, pulseBaseColor.g * intensity, pulseBaseColor.b * intensity, 1f);

            for (int i = 0; i < pulseRenderers.Length; i++)
            {
                Renderer r = pulseRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, hdr);
                propBlock.SetColor(ColorId, hdr);
                propBlock.SetColor(EmissionColorId, hdr);
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
