using UnityEngine;

public class GlassFill : MonoBehaviour
{
    [Header("Setup")]
    public Renderer liquidRenderer;                         // MeshRenderer on the Liquid child
    [Tooltip("Shader Graph property reference (from SG)")]
    public string fillProperty = "_FillAmount";             // your SG shows _FillAmount
    public float capacityMl = 250f;

    [Tooltip("Shader FillAmount.Y when empty")]
    public float emptyFillY = -0.40f;
    [Tooltip("Shader FillAmount.Y when full")]
    public float fullFillY  =  0.20f;

    [Header("Runtime")]
    public float currentMl = 0f;                            // for debugging/inspector
    public float smoothTime = 0.12f;

    // --- internals ---
    float targetMl, vel;
    int   fillID;
    Material mat;                                           // per-renderer material instance

    public bool IsFull  => currentMl >= capacityMl - 0.01f;
    public bool IsEmpty => currentMl <= 0.01f;

    void Awake()
    {
        if (!liquidRenderer) { Debug.LogError($"{name}: GlassFill missing liquidRenderer."); enabled = false; return; }

        // Make a unique material instance so we can safely drive the shader.
        mat    = liquidRenderer.material;                   // forces an instance on THIS renderer
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProperty) ? "_FillAmount" : fillProperty);

        // Force start empty.
        targetMl = currentMl = 0f;
        ApplyFill(0f);                                      // write to the shader now
    }

    void OnEnable()
    {
        // Ensure correct value if object is toggled during play.
        ApplyFill(Mathf.Clamp01(currentMl / Mathf.Max(0.0001f, capacityMl)));
    }

    void Update()
    {
        currentMl = Mathf.SmoothDamp(currentMl, targetMl, ref vel, smoothTime);
        ApplyFill(Mathf.Clamp01(currentMl / Mathf.Max(0.0001f, capacityMl)));
    }

    public void AddLiquid(float ml)    => targetMl = Mathf.Clamp(targetMl + ml, 0f, capacityMl);
    public void RemoveLiquid(float ml) => targetMl = Mathf.Clamp(targetMl - ml, 0f, capacityMl);

    void ApplyFill(float t01)
    {
        if (mat == null) return;
        float y = Mathf.Lerp(emptyFillY, fullFillY, t01);   // map 0..1 to shader-space height
        mat.SetVector(fillID, new Vector3(0f, y, 0f));
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep things in sync while editing.
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProperty) ? "_FillAmount" : fillProperty);
        if (liquidRenderer != null)
        {
            // In edit mode use sharedMaterial to avoid leaking instances; in play we already have 'mat'.
            if (!Application.isPlaying && liquidRenderer.sharedMaterial != null)
            {
                liquidRenderer.sharedMaterial.SetVector(fillID,
                    new Vector3(0f, Mathf.Lerp(emptyFillY, fullFillY, Mathf.Clamp01(currentMl / Mathf.Max(0.0001f, capacityMl))), 0f));
            }
        }
    }
#endif
}
