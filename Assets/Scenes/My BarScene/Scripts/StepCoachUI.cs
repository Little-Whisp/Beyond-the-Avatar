using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StepCoachUI : MonoBehaviour
{
    public ShakerContainer shaker;
    public GlassFill glass;

    [Header("Text")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI resetTipText;
    public CanvasGroup hintGroup;
    public float fadeSpeed = 8f;

    // ---- OUTLINE HIGHLIGHTS ----
    [Header("Step Highlights (assign targets)")]
    public GameObject[] pourTargets;
    public GameObject[] mixTargets;
    public GameObject[] serveTargets;

    [Tooltip("Turn on if your outline effect is driven by a Layer (via URP Renderer Feature).")]
    public bool useLayerOutline = true;
    [Tooltip("Layer name used by your outline renderer feature.")]
    public string outlineLayerName = "Outline";

    // ===== Quick Outline (component-based) =====
    [Header("Quick Outline Settings")]
    [Tooltip("If true, auto-add the QuickOutline component (global::Outline) at runtime when missing.")]
    public bool addOutlineIfMissing = true;

    [Tooltip("Outline Mode used by Quick Outline")]
    public Outline.Mode outlineMode = Outline.Mode.OutlineAll;

    [Tooltip("Outline color for highlights")]
    public Color outlineColor = Color.cyan;

    [Range(1f, 10f)]
    [Tooltip("Outline width for highlights")]
    public float outlineWidth = 6f;

    // Cache original layers to restore later (layer-based mode)
    private readonly Dictionary<GameObject, int> _originalLayers = new();

    enum Step { Pour, Mix, Serve }
    Step step;

    void OnEnable() { ResetBus.OnReset += OnReset; }
    void OnDisable() { ResetBus.OnReset -= OnReset; }

    void Start()
    {
        SetStep(Step.Pour, "Pour flavour");
        SetResetTipVisible(false);
    }

    void Update()
    {
        if (!shaker || !glass) return;

        Step target =
            (!shaker.HasLiquid || !shaker.IsFull) ? Step.Pour :
            (!shaker.mixed) ? Step.Mix :
                                                      Step.Serve;

        if (step != target)
            SetStep(target, DefaultHintFor(target));

        switch (step)
        {
            case Step.Pour:
                ShowHint("Pour flavour");
                SetResetTipVisible(false);
                break;
            case Step.Mix:
                ShowHint(shaker.IsSealed ? "Shake to mix" : "Seal lid");
                SetResetTipVisible(true);
                break;
            case Step.Serve:
                ShowHint("Pour into glass");
                SetResetTipVisible(true);
                break;
        }
    }

    void OnReset()
    {
        SetStep(Step.Pour, "Reset! Pour flavour");
        Invoke(nameof(ClearResetFlash), 1f);
    }
    void ClearResetFlash() => ShowHint("Pour flavour");

    // ---------- helpers ----------
    void SetStep(Step s, string hint)
    {
        step = s;
        ApplyHighlightsFor(s);
        ShowHint(hint);
    }

    string DefaultHintFor(Step s) => s switch
    {
        Step.Pour => "Pour flavour",
        Step.Mix => "Seal & shake",
        _ => "Uncap & pour"
    };

    // ================== HIGHLIGHT LOGIC ==================
    void ApplyHighlightsFor(Step s)
    {
        // 1) Clear all previous highlights
        ClearAllHighlights();

        // 2) Enable highlights for this step
        GameObject[] targets = s switch
        {
            Step.Pour => pourTargets,
            Step.Mix => mixTargets,
            Step.Serve => serveTargets,
            _ => null
        };

        if (targets == null) return;

        if (useLayerOutline)
        {
            int outlineLayer = LayerMask.NameToLayer(outlineLayerName);
            if (outlineLayer < 0)
            {
                Debug.LogWarning($"Outline layer '{outlineLayerName}' not found. Create it in Project Settings > Tags & Layers.");
                return;
            }

            foreach (var go in targets)
            {
                if (!go) continue;
                // store original layer of this object and all children
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.gameObject) continue;
                    if (!_originalLayers.ContainsKey(t.gameObject))
                        _originalLayers.Add(t.gameObject, t.gameObject.layer);
                    t.gameObject.layer = outlineLayer;
                }
            }
        }
        else
        {
            // Component-based highlight using Chris Nolet's Quick Outline (class name: global::Outline).
            foreach (var go in targets)
            {
                if (!go) continue;

                // Prefer the 3D QuickOutline, not the UI Outline
                var qo = go.GetComponent<global::Outline>();
                if (!qo && addOutlineIfMissing)
                    qo = go.AddComponent<global::Outline>();

                if (qo)
                {
                    qo.OutlineMode = outlineMode;
                    qo.OutlineColor = outlineColor;
                    qo.OutlineWidth = outlineWidth;
                    qo.enabled = true;
                }

                // Also enable on children that already have QuickOutline
                foreach (var child in go.GetComponentsInChildren<Behaviour>(true))
                {
                    if (!child) continue;

                    // Skip UI Outline (UnityEngine.UI.Outline)
                    if (child is UnityEngine.UI.Outline) continue;

                    // Only touch QuickOutline (class name "Outline" in global namespace)
                    if (child.GetType().Name == "Outline" &&
                        (child.GetType().Namespace == null || child.GetType().Namespace == ""))
                    {
                        try
                        {
                            var q = (global::Outline)child;
                            q.OutlineMode = outlineMode;
                            q.OutlineColor = outlineColor;
                            q.OutlineWidth = outlineWidth;
                        }
                        catch { /* safe no-op if not QuickOutline */ }

                        child.enabled = true;
                    }
                }
            }
        }
    }

    void ClearAllHighlights()
    {
        if (useLayerOutline)
        {
            // restore original layers
            if (_originalLayers.Count > 0)
            {
                foreach (var kvp in _originalLayers)
                    if (kvp.Key) kvp.Key.layer = kvp.Value;
                _originalLayers.Clear();
            }
        }
        else
        {
            // turn off QuickOutline components under all step targets
            DisableOutlinesIn(pourTargets);
            DisableOutlinesIn(mixTargets);
            DisableOutlinesIn(serveTargets);
        }
    }

    void DisableOutlinesIn(GameObject[] roots)
    {
        if (roots == null) return;

        foreach (var go in roots)
        {
            if (!go) continue;

            // Disable QuickOutline on the root if present
            var qo = go.GetComponent<global::Outline>();
            if (qo) qo.enabled = false;

            // Disable QuickOutline on children (but not Unity UI Outline)
            foreach (var b in go.GetComponentsInChildren<Behaviour>(true))
            {
                if (!b) continue;
                if (b is UnityEngine.UI.Outline) continue; // leave UI graphics alone

                if (b.GetType().Name == "Outline" &&
                    (b.GetType().Namespace == null || b.GetType().Namespace == ""))
                {
                    b.enabled = false;
                }
            }
        }
    }
    // =====================================================

    // ---------- text ----------
    void ShowHint(string msg)
    {
        if (hintText) hintText.text = msg;
        if (hintGroup)
            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
    }

    void SetResetTipVisible(bool on)
    {
        if (resetTipText)
            resetTipText.text = on ? "Reset? Throw it away." : "";
    }
}
