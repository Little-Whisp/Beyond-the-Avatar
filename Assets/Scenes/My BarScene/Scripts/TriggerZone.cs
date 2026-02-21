using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

public class TriggerZone : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool isGlassZone = true;
    public string drinkTag = "Cocktail";

    [Header("Prompt System")]
    public PromptTrigger promptTrigger;

    [Header("Player Base (dynamic)")]
    public PlayerBaseTrigger playerBase;

    [Header("Visual (Highlight)")]
    public GameObject zoneVisual;

    [Header("Avatar Image (optional)")]
    public GameObject avatarImageVisual;   // assign a world-space image object here (per zone)

    [Header("Serve VFX (local)")]
    public GameObject servePoofVfx;
    public float destroyDelay = 0f;

    public Transform glassAnchor;

    private GameObject lastPlacedGlass;
    private bool _occupied;

    [Header("Avatar Choice Spot")]
    public bool isAvatarChoiceSpot = false; // enable only on 3 table spots
    public AvatarType avatarType = AvatarType.Realistic;

    [HideInInspector]
    public BartenderPromptSession session;

    private void Awake()
    {
        ForceHide();
    }

    private void OnEnable()
    {
        ForceHide();
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

    private void OnNetworkSpawn()
    {
        ForceHide();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_occupied) return;
        if (!isGlassZone) return;

        var go = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!go.CompareTag(drinkTag)) return;

        lastPlacedGlass = go;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_occupied) return;
        if (!isGlassZone) return;

        var go = other.attachedRigidbody
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!go.CompareTag(drinkTag)) return;

        var grab = go.GetComponentInChildren<XRGrabInteractable>(true);
        if (grab && grab.isSelected)
            return;

        // _occupied = true;
        // HandleGlassPlacement(go);
        StartCoroutine(DelayedPlacement(go));
    }

    private System.Collections.IEnumerator DelayedPlacement(GameObject go)
    {
        yield return null; // wait one frame

        var grab = go.GetComponentInChildren<XRGrabInteractable>(true);
        if (grab && grab.isSelected)
        {
            _occupied = false;
            yield break;
        }

        _occupied = true;
        HandleGlassPlacement(go);
    }

    // ✅ NEW helper: spawns poof + destroys the cocktail root
    private void PoofAndDestroyDrink(GameObject glass)
    {
        Debug.Log($"[TriggerZone] PoofAndDestroyDrink CALLED for {glass.name}");

        Vector3 poofPos = glassAnchor != null
            ? glassAnchor.position
            : glass.transform.position;

        // 🔴 Disable GlassPickup FIRST
        var pickup = glass.GetComponent<GlassPickup>();
        if (pickup != null)
        {
            pickup.enabled = false;
        }

        // 🔴 Disable grabbing
        var grab = glass.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = false;
        }

        // 🔴 Disable physics
        var rb = glass.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 🔴 Disable collider
        var col = glass.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // 🔵 Spawn 
        if (servePoofVfx != null)
            Instantiate(servePoofVfx, poofPos, Quaternion.identity);

        Debug.Log($"[TriggerZone] Destroying {glass.name} at time {Time.time}");

        Destroy(glass);
    }

    private void HandleGlassPlacement(GameObject glass)
    {

        Debug.Log($"[TriggerZone] HandleGlassPlacement START for {glass.name}");

        lastPlacedGlass = glass;

        if (glassAnchor != null)
        {
            glass.transform.position = glassAnchor.position;
            glass.transform.rotation = glassAnchor.rotation;
        }

        var rb = glass.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;

            // StartCoroutine(ReleasePhysicsNextFrame(rb));
        }

        ForceHide();

        // ✅ If this is an avatar choice spot, keep your host-only logic
        if (isAvatarChoiceSpot)
        {
            PoofAndDestroyDrink(glass);

            session?.LocalAdvanceAfterServe(avatarType);

            promptTrigger?.promptGenerator?.ShowNextPrompt();

            _occupied = false;
            return;
        }


        // --- Legacy flow (non-choice zones) ---
        // ✅ ALSO: poof + destroy cocktail here
        PoofAndDestroyDrink(glass);

        string owner = playerBase != null ? playerBase.assignedPlayerName : "Unknown";
        string avatar = playerBase != null ? playerBase.assignedAvatarType : "Unknown";
        string prompt = promptTrigger?.promptGenerator?.currentPrompt ?? "UnknownPrompt";

        Debug.Log($"[DATA] Player: {owner} | Avatar: {avatar} | Prompt: {prompt}");

        GameManager.Instance?.LogEvent(
            $"Served -> Player: {owner} | Avatar: {avatar} | Prompt: {prompt}"
        );

        // promptTrigger?.ResetPrompt();
        promptTrigger?.ResetPrompt();
        promptTrigger?.promptGenerator?.ShowNextPrompt();


        _occupied = false;
    }

    private System.Collections.IEnumerator ReleasePhysicsNextFrame(Rigidbody rb)
    {
        yield return null;

        if (!rb) yield break;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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
