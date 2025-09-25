using UnityEngine;
using System.Collections;

public class BreakableRespawn : MonoBehaviour
{
    public enum RespawnStyle { BreakFX, Poof }
    public enum TriggerMode { OnFloorContact, Thresholds }

    [Header("Behavior")]
    public RespawnStyle style = RespawnStyle.BreakFX;
    public TriggerMode triggerMode = TriggerMode.OnFloorContact;
    public float breakImpulse = 6f;
    public float minSpeed = 2f;
    public float respawnDelay = 0.5f;

    [Header("Floor detection (tag-only)")]
    public string floorTag = "Floor";

    [Header("FX (optional)")]
    public GameObject breakShardsPrefab;   // for BreakFX (glass)
    public GameObject poofPrefab;          // for Poof
    public AudioClip breakClip;
    public float fxScale = 1f;

    [Header("Respawn")]
    [Tooltip("Empty Transform where this should respawn. If empty, we use the captured world pose.")]
    public Transform respawnAnchor;

    // captured world pose
    Vector3 respawnWorldPos;
    Quaternion respawnWorldRot;

    Rigidbody rb;
    Collider[] colliders;
    Renderer[] renderers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);

        // capture starting world pose
        SetRespawnHere();
    }

    [ContextMenu("Set Respawn = Here")]
    public void SetRespawnHere()
    {
        respawnWorldPos = transform.position;
        respawnWorldRot = transform.rotation;
    }

    [ContextMenu("Respawn Now")]
    public void RespawnNow()
    {
        // if someone assigned the same object as the anchor, ignore it
        bool useAnchor = respawnAnchor && respawnAnchor != transform;

        transform.SetPositionAndRotation(
            useAnchor ? respawnAnchor.position : respawnWorldPos,
            useAnchor ? respawnAnchor.rotation : respawnWorldRot
        );

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    void OnCollisionEnter(Collision c)
    {
        bool shouldBreak = false;

        if (triggerMode == TriggerMode.OnFloorContact)
        {
            shouldBreak = c.collider.CompareTag(floorTag);
        }
        else
        {
            float impulse = c.impulse.magnitude;
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            shouldBreak = impulse >= breakImpulse || speed >= minSpeed;
        }

        if (shouldBreak)
        {
            Vector3 fxPos = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            StartCoroutine(BreakAndRespawn(fxPos));
        }
    }

    IEnumerator BreakAndRespawn(Vector3 fxPos)
    {
        // FX
        if (style == RespawnStyle.BreakFX && breakShardsPrefab)
            SpawnFX(breakShardsPrefab, fxPos);
        else if (style == RespawnStyle.Poof && poofPrefab)
            SpawnFX(poofPrefab, transform.position);

        if (breakClip) AudioSource.PlayClipAtPoint(breakClip, transform.position);

        // hide WITHOUT disabling the GameObject (keeps coroutine alive)
        HideObject();

        yield return new WaitForSeconds(respawnDelay);

        RespawnNow();
        ShowObject();
    }

    void HideObject()
    {
        foreach (var r in renderers) if (r) r.enabled = false;
        foreach (var c in colliders) if (c) c.enabled = false;
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ShowObject()
    {
        foreach (var c in colliders) if (c) c.enabled = true;
        foreach (var r in renderers) if (r) r.enabled = true;
        if (rb) rb.isKinematic = false;
    }

    void SpawnFX(GameObject prefab, Vector3 pos)
    {
        var fx = Instantiate(prefab, pos, Quaternion.identity);
        fx.transform.localScale *= fxScale;
        var ps = fx.GetComponent<ParticleSystem>();
        if (ps) Destroy(fx, ps.main.duration + ps.main.startLifetimeMultiplier + 0.5f);
        else Destroy(fx, 3f);
    }
}
