using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class StepCoachUI : MonoBehaviour
{
    public ShakerContainer shaker;
    public GlassFill glass;

    public Image dotPour, dotMix, dotServe;
    public float inactiveAlpha = 0.35f;

#if TMP_PRESENT
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI resetTipText;
#endif
    public CanvasGroup hintGroup;
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
        Invoke(nameof(ClearResetFlash), 1.0f);
    }
    void ClearResetFlash() => ShowHint("Pour flavour");

    // ---------- helpers ----------
    void SetStep(Step s, string hint) { step = s; SetDots(s); ShowHint(hint); }
    string DefaultHintFor(Step s) => s switch
    {
        Step.Pour  => "Pour flavour",
        Step.Mix   => "Seal & shake",
        _          => "Uncap & pour"
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
        var c = img.color; c.a = on ? 1f : inactiveAlpha; img.color = c;
        img.transform.localScale = Vector3.Lerp(img.transform.localScale, on ? Vector3.one*1.1f : Vector3.one, 0.25f);
    }
    void ShowHint(string msg)
    {
#if TMP_PRESENT
        if (hintText) hintText.text = msg;
#endif
        if (hintGroup) hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, 1f, fadeSpeed*Time.deltaTime);
    }
    void SetResetTipVisible(bool on)
    {
#if TMP_PRESENT
        if (resetTipText) resetTipText.text = on ? "Reset? Throw it away." : "";
#endif
    }
}
