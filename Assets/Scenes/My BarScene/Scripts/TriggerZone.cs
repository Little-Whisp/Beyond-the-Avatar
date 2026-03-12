using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool isGlassZone = true;
    public string drinkTag = "Cocktail";

    [Header("Prompt System")]
    public PromptTrigger promptTrigger;

    [Header("Visual (Highlight)")]
    public GameObject zoneVisual;
    public GameObject avatarImageVisual;

    [Header("Serve VFX (local)")]
    public GameObject servePoofVfx;

    [Header("Avatar Choice Spot")]
    public bool isAvatarChoiceSpot = false;
    public AvatarType avatarType = AvatarType.Realistic;

    [HideInInspector]
    public BartenderPromptSession session;

    private bool _triggered = false;

    private void Awake()
    {
        ForceHide();
    }

    private void OnEnable()
    {
        ForceHide();
        _triggered = false;
    }

    private void Start()
    {
        Debug.Log($"[TriggerZone] START running on {name}", this);
        ForceHide();
    }

    public void ForceHide()
    {
        if (zoneVisual != null)
            zoneVisual.SetActive(false);

        if (avatarImageVisual != null)
            avatarImageVisual.SetActive(false);

        var ps = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGlassZone) return;
        if (_triggered) return;

        var go = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!go.CompareTag(drinkTag)) return;

        _triggered = true;

        Debug.Log("[TriggerZone] Drink detected");

        ForceHide();

        if (servePoofVfx != null)
        {
            Instantiate(servePoofVfx, transform.position, Quaternion.identity);
        }

        string prompt = promptTrigger?.promptGenerator?.currentPrompt ?? "UnknownPrompt";

        if (isAvatarChoiceSpot)
        {
            session?.LocalAdvanceAfterServe(avatarType);
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCocktailServed(
                    avatarType.ToString(),
                    "",
                    prompt
                );
            }
            else
            {
                Debug.LogError("[TriggerZone] GameManager.Instance is NULL");
            }
        }

        Destroy(go);

        promptTrigger?.promptGenerator?.ShowNextPrompt();

    }

    public void ShowHighlight()
    {
        if (zoneVisual == null) return;

        zoneVisual.SetActive(true);

        if (avatarImageVisual != null)
            avatarImageVisual.SetActive(true);

        var ps = zoneVisual.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps)
        {
            p.gameObject.SetActive(true);
            p.Play(true);
        }
    }

    public void HideHighlight()
    {
        ForceHide();
    }

    public void SetSession(BartenderPromptSession s)
    {
        session = s;
    }
}