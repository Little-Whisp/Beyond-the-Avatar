using UnityEngine;
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

    public Transform glassAnchor;

    private GameObject lastPlacedGlass;

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

    // ✅ Force everything visual OFF (even if zoneVisual was assigned wrong)
    public void ForceHide()
    {
        // hide the assigned object
        if (zoneVisual != null)
            zoneVisual.SetActive(false);

        // ALSO stop + disable any particles under this zone
        var ps = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.gameObject.SetActive(false);
        }
    }

    // ⚠️ NOTE:
    // OnNetworkSpawn() ONLY runs if this script inherits NetworkBehaviour.
    // Right now this method will NOT be called by Netcode.
    private void OnNetworkSpawn()
    {
        ForceHide();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGlassZone || !other.CompareTag(drinkTag))
            return;

        // ✅ If it's currently being held, don't "place" it (prevents re-kinematic)
        var grab = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        HandleGlassPlacement(other.gameObject);
    }


    private void HandleGlassPlacement(GameObject glass)
    {
        lastPlacedGlass = glass;

        if (glassAnchor != null)
        {
            glass.transform.position = glassAnchor.position;
            glass.transform.rotation = glassAnchor.rotation;
        }

        var rb = glass.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;                 // lock it in place on the anchor
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Hide highlight again after serving
        ForceHide();

        string owner = playerBase != null ? playerBase.assignedPlayerName : "Unknown";
        string avatar = playerBase != null ? playerBase.assignedAvatarType : "Unknown";
        string prompt = promptTrigger?.promptGenerator?.currentPrompt ?? "UnknownPrompt";

        Debug.Log($"[DATA] Player: {owner} | Avatar: {avatar} | Prompt: {prompt}");

        GameManager.Instance?.LogEvent(
            $"Served -> Player: {owner} | Avatar: {avatar} | Prompt: {prompt}"
        );

        promptTrigger?.ResetPrompt();
    }

    public void ShowHighlight()
    {
        if (zoneVisual == null) return;

        zoneVisual.SetActive(true);

        // START the highlight particles even if Play On Awake is off
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
}
