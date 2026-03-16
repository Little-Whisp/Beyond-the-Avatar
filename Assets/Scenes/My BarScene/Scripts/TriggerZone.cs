using TMPro;
using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool isGlassZone = true;
    public string drinkTag = "Cocktail";

    [Header("Prompt System")]
    public PromptTrigger promptTrigger;

    [Header("Serve Audio")]
    public AudioSource audioSource;
    public AudioClip serveSound;

    [Header("Visual (Highlight)")]
    public GameObject zoneVisual;
    public GameObject avatarImageVisual;

    [Header("Zone Text")]
    public TextMeshProUGUI zoneText;

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

        if (zoneText != null)
            zoneText.gameObject.SetActive(false);

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
            Vector3 pos = transform.position + Vector3.up * 0.1f;
            GameObject vfx = Instantiate(servePoofVfx, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        if (audioSource != null && serveSound != null)
        {
            audioSource.PlayOneShot(serveSound);
        }

        string prompt = promptTrigger?.promptGenerator?.currentPrompt ?? "UnknownPrompt";

        if (isAvatarChoiceSpot)
        {
            session?.LocalAdvanceAfterServe(avatarType);
        }
        else
        {
            float soda = 0f;
            float hot = 0f;
            float strawberry = 0f;

            if (GameManager.Instance != null && GameManager.Instance.shaker != null)
            {
                var shaker = GameManager.Instance.shaker;

                float total = shaker.lastSoda + shaker.lastHot + shaker.lastStrawberry;

                if (total > 0f)
                {
                    soda = Mathf.Round((shaker.lastSoda / total) * 100f);
                    hot = Mathf.Round((shaker.lastHot / total) * 100f);
                    strawberry = Mathf.Round((shaker.lastStrawberry / total) * 100f);
                }

                Debug.Log($"Drink recipe captured → Soda:{soda}% Hot:{hot}% Strawberry:{strawberry}%");
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCocktailServed(
                    avatarType.ToString(),
                    prompt,
                    soda,
                    hot,
                    strawberry
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

        if (zoneText != null)
            zoneText.gameObject.SetActive(true);

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