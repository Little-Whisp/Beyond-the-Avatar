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

    public Transform glassAnchor;

    private GameObject lastPlacedGlass;

    private bool _occupied;


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

        var ps = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in ps)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.gameObject.SetActive(false);
        }
    }

    // NOTE: Only runs if this inherits NetworkBehaviour (it doesn't right now)
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

        _occupied = true;
        HandleGlassPlacement(go);
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
            // ✅ lock briefly so it "snaps" cleanly
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;

            // ✅ IMPORTANT: release physics again so it can be grabbed normally
            // (matches how your other bar items behave)
            StartCoroutine(ReleasePhysicsNextFrame(rb));
        }

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

    private System.Collections.IEnumerator ReleasePhysicsNextFrame(Rigidbody rb)
    {
        // wait a frame so the snap transform settles
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
