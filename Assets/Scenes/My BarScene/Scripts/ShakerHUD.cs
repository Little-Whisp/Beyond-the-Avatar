using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class ShakerHUD : MonoBehaviour
{
    [Header("Bars")]
    public Image barOld;
    public Image barLife;
    public Image barImp;

#if TMP_PRESENT
    public TextMeshProUGUI mlOldTxt;
    public TextMeshProUGUI mlLifeTxt;
    public TextMeshProUGUI mlImpTxt;
#endif

    [Header("Mix UI")]
    public Image mixRing;           // radial fill 0..1
    public GameObject mixedBadge;   // enable when mixed

    [Header("Look & Feel")]
    public CanvasGroup group;       // fade when empty
    public float fadeSpeed = 8f;

    [Header("Colors (optional)")]
    public FlavorPalette palette;

    float targetAlpha = 0.2f;

    void Awake()
    {
        // Set bar colors from the palette (optional)
        if (palette)
        {
            if (barOld)  barOld.color  = palette.oldTwitter;
            if (barLife) barLife.color = palette.lifeJacket;
            if (barImp)  barImp.color  = palette.imposter;
        }
    }

    void Update()
    {
        if (group)
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    // Called by ShakerContainer whenever volumes change
    public void SetValues(float mlOld, float mlLife, float mlImp, float capacity)
    {
        float cap = Mathf.Max(0.0001f, capacity);
        if (barOld)  barOld.fillAmount  = Mathf.Clamp01(mlOld  / cap);
        if (barLife) barLife.fillAmount = Mathf.Clamp01(mlLife / cap);
        if (barImp)  barImp.fillAmount  = Mathf.Clamp01(mlImp  / cap);

#if TMP_PRESENT
        if (mlOldTxt)  mlOldTxt.text  = $"{mlOld:0} ml";
        if (mlLifeTxt) mlLifeTxt.text = $"{mlLife:0} ml";
        if (mlImpTxt)  mlImpTxt.text  = $"{mlImp:0} ml";
#endif
        targetAlpha = (mlOld + mlLife + mlImp) > 0.01f ? 1f : 0.2f;
    }

    // Called as you shake; p = 0..1
    public void SetMixed(bool mixed, float p)
    {
        if (mixRing) mixRing.fillAmount = Mathf.Clamp01(p);
        if (mixedBadge) mixedBadge.SetActive(mixed);
    }
}
