using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ReturnToSpawn : MonoBehaviour
{
    [Header("Snap")]
    public float returnSpeed = 10f;
    [Tooltip("Tag on the bar surface (or proxy) that qualifies for snapping.")]
    public string barTag = "Bar";

    [Header("Detection (on release)")]
    public float probeDown = 0.6f;       // try 0.6 so tall pivots still hit
    public float probeRadius = 0.25f;    // a bit wider for bottles
    public float probeYOffset = 0.05f;   // start slightly above pivot

    [Header("Debug")]
    public bool debugLogs = false;

    Vector3 spawnPos;
    Quaternion spawnRot;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    Rigidbody rb;
    Collider col;

    void Awake()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb   = GetComponent<Rigidbody>();
        col  = GetComponent<Collider>();
        col.isTrigger = false;

        grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (grab) grab.selectExited.RemoveListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs _)
    {
        if (!IsOverBarNow())
        {
            if (debugLogs) Debug.Log($"{name}: released not over bar → no snap");
            return;
        }

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        StartCoroutine(SnapBack());
    }

    System.Collections.IEnumerator SnapBack()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPos, spawnPos, t);
            transform.rotation = Quaternion.Slerp(startRot, spawnRot, t);
            yield return null;
        }

        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (rb) rb.isKinematic = false;
    }

    bool IsOverBarNow()
    {
        Vector3 origin = transform.position + Vector3.up * probeYOffset;

        // 1) Raycast down (hits triggers too)
        if (Physics.Raycast(origin, Vector3.down, out var hit, probeDown + probeYOffset, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag(barTag))
            {
                if (debugLogs) Debug.Log($"{name}: ray hit {hit.collider.name} (tag {barTag})");
                return true;
            }
        }

        // 2) Fallback overlap sphere
        var hits = Physics.OverlapSphere(origin, probeRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (h.CompareTag(barTag))
            {
                if (debugLogs) Debug.Log($"{name}: overlap found {h.name} (tag {barTag})");
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * probeYOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * (probeDown + probeYOffset));
        Gizmos.DrawWireSphere(origin, probeRadius);
    }
#endif
}
