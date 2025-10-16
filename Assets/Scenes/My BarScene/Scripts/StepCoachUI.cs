using UnityEngine;
using UnityEngine.UI;
using TMPro; // <- always include

public class StepCoachUI : MonoBehaviour
{
    public ShakerContainer shaker;
    public GlassFill glass;

    [Header("Dots")]
    public Image dotPour, dotMix, dotServe;
    [Range(0f,1f)] public float inactiveAlpha = 0.35f;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.white;

    [Header("Text")]
    public TextMeshProUGUI hintText;      // drag TextCanvas/HintText
    public TextMeshProUGUI resetTipText;  // drag TextCanvas/ResetTipText
    public CanvasGroup hintGroup;         // drag TextCanvas (CanvasGroup)
    public float fadeSpeed = 8f;

    enum Step { Pour, Mix, Serve }
    Step step;

    void OnEnable()  { ResetBus.OnReset += OnReset; }
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
            (!shaker.mixed)                        ? Step.Mix  :
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
                ShowHint(shaker.IsSealed && shaker.IsCapped ? "Shake to mix" : "Seal lid + cap");
                SetResetTipVisible(true);
                break;

            case Step.Serve:
                ShowHint(shaker.IsCapped ? "Remove cap" : "Pour into glass");
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
    void SetStep(Step s, string hint) { step = s; SetDots(s); ShowHint(hint); }
    string DefaultHintFor(Step s) => s switch
    {
        Step.Pour => "Pour flavour",
        Step.Mix  => "Seal & shake",
        _         => "Uncap & pour"
    };

    void SetDots(Step active)
    {
        SetDot(dotPour,  active == Step.Pour);
        SetDot(dotMix,   active == Step.Mix);
        SetDot(dotServe, active == Step.Serve);
    }

    void SetDot(Image img, bool on)
    {
        if (!img) return;
        var off = new Color(inactiveColor.r, inactiveColor.g, inactiveColor.b, inactiveAlpha);
        img.color = on ? activeColor : off;
        img.transform.localScale = Vector3.Lerp(
            img.transform.localScale,
            on ? Vector3.one * 1.1f : Vector3.one,
            0.25f
        );
    }

    void ShowHint(string msg)
    {
        if (hintText) hintText.text = msg;
        if (hintGroup) hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
    }

    void SetResetTipVisible(bool on)
    {
        if (resetTipText) resetTipText.text = on ? "Reset? Throw it away." : "";
    }
}
