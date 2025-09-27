using UnityEngine;
using System.Collections;

public class BreakableDestroy : MonoBehaviour
{
    public enum DestroyStyle { BreakFX, Poof }
    public enum TriggerMode { OnFloorContact, Thresholds }

    [Header("Behavior")]
    public DestroyStyle style = DestroyStyle.BreakFX;
    public TriggerMode triggerMode = TriggerMode.OnFloorContact;
    public float breakImpulse = 6f;
    public float minSpeed = 2f;

    [Header("Floor detection (tag-only)")]
    public string floorTag = "Floor";

    [Header("FX (optional)")]
    public GameObject breakShardsPrefab;
    public GameObject poofPrefab;
    public AudioClip breakClip;
    public float fxScale = 1f;

    Rigidbody rb;
    Collider[] colliders;
    Renderer[] renderers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
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
            float speed = rb ? rb.linearVelocity.magnitude : 0f;   // <-- fixed
            shouldBreak = impulse >= breakImpulse || speed >= minSpeed;
        }

        if (shouldBreak)
        {
            Vector3 fxPos = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            StartCoroutine(BreakAndDestroy(fxPos));
        }
    }

    IEnumerator BreakAndDestroy(Vector3 fxPos)
    {
        if (style == DestroyStyle.BreakFX && breakShardsPrefab)
            SpawnFX(breakShardsPrefab, fxPos);
        else if (style == DestroyStyle.Poof && poofPrefab)
            SpawnFX(poofPrefab, transform.position);

        if (breakClip) AudioSource.PlayClipAtPoint(breakClip, transform.position);

        HideObject();
        yield return null;
        Destroy(gameObject);
    }

    void HideObject()
    {
        foreach (var r in renderers) if (r) r.enabled = false;
        foreach (var c in colliders) if (c) c.enabled = false;

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;        // <-- fixed
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
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
