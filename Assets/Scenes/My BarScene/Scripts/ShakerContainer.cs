using UnityEngine;

public class ShakerContainer : MonoBehaviour
{
    [Header("Capacity")]
    public float capacityMl = 500f;

    [Header("Liquid visuals (shader)")]
    public Renderer liquidRenderer;
    public string fillProp = "_FillAmount";   // Vector3/4: y = height
    public string colorProp = "_BaseColor";   // your shader's color property
    public float emptyY = -0.40f, fullY = 0.20f;

    [Header("Meters (optional worldspace UI)")]
    public ShakerHUD hud;

    [Header("Mixing")]
    public bool mixed;                        // true after enough shake energy
    public float mixProgress;                 // 0..1 (progress bar)
    public float mixNeeded = 1.0f;            // total energy required

    [Header("Colors")]
    public FlavorPalette palette;             // set in Inspector

    [Header("State (for pour rules)")]
    public bool IsSealed;                     // lid snapped on/off
    public bool IsCapped;                     // spout cap on/off (if used)

    // ✅ NEW: Audio settings
    [Header("Audio")]
    [Tooltip("AudioSource on this shaker (or child object) to play sounds.")]
    public AudioSource audioSource;
    [Tooltip("Sound to play once when shaker becomes full.")]
    public AudioClip fullSound;

    // volumes (ml)
    public float mlOld, mlLife, mlImp;

    // internals
    Material mat;
    int fillID, colorID;

    // ✅ Property to check if there's liquid
    public bool HasLiquid => TotalMl > 0.01f;

    void Awake()
    {
        if (liquidRenderer) mat = liquidRenderer.material;
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProp) ? "_FillAmount" : fillProp);
        colorID = Shader.PropertyToID(string.IsNullOrWhiteSpace(colorProp) ? "_BaseColor" : colorProp);
        ApplyVisuals();
    }

    public float TotalMl => mlOld + mlLife + mlImp;
    public bool IsFull => TotalMl >= capacityMl - 0.01f;
    public bool CanMix => IsSealed && IsCapped;

    // ---------- Adding liquid from bottles ----------
    public void AddFlavor(Flavor f, float ml)
    {
        if (ml <= 0f || IsFull) return;

        bool wasFullBefore = IsFull; // track state before adding

        float room = Mathf.Max(0, capacityMl - TotalMl);
        ml = Mathf.Min(ml, room);
        if (ml <= 0f) return;

        switch (f)
        {
            case Flavor.OldTwitter: mlOld += ml; break;
            case Flavor.LifeJacket: mlLife += ml; break;
            default: mlImp += ml; break;
        }

        // New ingredient → must remix
        mixed = false;
        mixProgress = 0f;

        ApplyVisuals();
        hud?.SetValues(mlOld, mlLife, mlImp, capacityMl);
        hud?.SetMixed(false, 0f);

        // ✅ Play full sound if we just became full
        if (!wasFullBefore && IsFull && audioSource && fullSound)
        {
            audioSource.PlayOneShot(fullSound);
        }
    }

    // ---------- Shake to mix ----------
    public void AddMixEnergy(float amount)
    {
        if (TotalMl <= 0f || mixed) return;

        mixProgress += Mathf.Max(0f, amount);
        float p = Mathf.Clamp01(mixProgress / Mathf.Max(0.0001f, mixNeeded));
        if (p >= 1f) mixed = true;

        hud?.SetMixed(mixed, p);
        ApplyVisuals();
    }

    // ---------- Drain when pouring ----------
    public float Drain(float ml)
    {
        float available = Mathf.Max(0f, TotalMl);
        if (available <= 0f || ml <= 0f) return 0f;

        float take = Mathf.Min(ml, available);

        float t = Mathf.Max(0.0001f, available);
        float po = mlOld / t;
        float pl = mlLife / t;
        float pi = mlImp / t;

        mlOld = Mathf.Max(0f, mlOld - take * po);
        mlLife = Mathf.Max(0f, mlLife - take * pl);
        mlImp = Mathf.Max(0f, mlImp - take * pi);

        ApplyVisuals();
        hud?.SetValues(mlOld, mlLife, mlImp, capacityMl);

        return take;
    }

    public (float o, float l, float i) Percentages()
    {
        float t = Mathf.Max(0.0001f, TotalMl);
        return (mlOld / t, mlLife / t, mlImp / t);
    }

    public Color MixColor()
    {
        var (po, pl, pi) = Percentages();
        Color co = palette ? palette.oldTwitter : Color.cyan;
        Color cl = palette ? palette.lifeJacket : Color.yellow;
        Color ci = palette ? palette.imposter : Color.magenta;
        return co * po + cl * pl + ci * pi;
    }

    void ApplyVisuals()
    {
        if (!mat) return;

        float t = Mathf.Clamp01(TotalMl / Mathf.Max(0.0001f, capacityMl));
        float y = Mathf.Lerp(emptyY, fullY, t);
        mat.SetVector(fillID, new Vector3(0, y, 0));

        if (mixed)
            mat.SetColor(colorID, MixColor());
    }

    public void Clear()
    {
        mlOld = mlLife = mlImp = 0f;
        mixed = false; mixProgress = 0f;
        ApplyVisuals();
        hud?.SetValues(0, 0, 0, capacityMl);
        hud?.SetMixed(false, 0);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!liquidRenderer) return;
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProp) ? "_FillAmount" : fillProp);
        colorID = Shader.PropertyToID(string.IsNullOrWhiteSpace(colorProp) ? "_BaseColor" : colorProp);

        var m = Application.isPlaying ? liquidRenderer.material : liquidRenderer.sharedMaterial;
        if (!m) return;

        float t = Mathf.Clamp01(TotalMl / Mathf.Max(0.0001f, capacityMl));
        float y = Mathf.Lerp(emptyY, fullY, t);
        m.SetVector(fillID, new Vector3(0, y, 0));
    }
#endif
}
