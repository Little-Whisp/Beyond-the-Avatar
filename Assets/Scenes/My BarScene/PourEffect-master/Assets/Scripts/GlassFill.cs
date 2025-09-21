using UnityEngine;

public class GlassFill : MonoBehaviour {
    [Header("Setup")]
    public Transform liquid;              // child mesh pivoted at bottom
    public float capacityMl = 250f;
    public float liquidHeightAtFull = 0.12f; // meters when full

    [Header("Runtime")]
    public float currentMl = 0f;
    public float smoothTime = 0.12f;

    float targetMl, vel;

    void Awake() {
        targetMl = currentMl;
        UpdateVisuals(true);
    }

    public void AddLiquid(float ml) {
        targetMl = Mathf.Clamp(targetMl + ml, 0f, capacityMl);
    }

    void Update() {
        currentMl = Mathf.SmoothDamp(currentMl, targetMl, ref vel, smoothTime);
        UpdateVisuals();
    }

    void UpdateVisuals(bool instant=false) {
        float fill01 = Mathf.Clamp01(currentMl / capacityMl);
        float h = fill01 * liquidHeightAtFull;

        if (liquid != null) {
            Vector3 s = liquid.localScale;
            s.y = Mathf.Max(0.0001f, h / Mathf.Max(0.0001f, liquidHeightAtFull));
            liquid.localScale = s;
        }
    }
}
