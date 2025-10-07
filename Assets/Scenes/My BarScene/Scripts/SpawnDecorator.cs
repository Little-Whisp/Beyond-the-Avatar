using System.Collections;
using UnityEngine;


public class SpawnDecorator : MonoBehaviour
{
    [Header("Floor detection")]
    public string floorTag = "Floor";

    [Header("Break settings")]
    public GameObject breakShardsPrefab;
    public AudioClip breakClip;
    public float breakImpulse = 8f;
    public float minSpeed = 3f;
    public float breakFxScale = 1f;

    [Tooltip("Arm delay so a just-spawned drink doesn’t instantly break.")]
    public float noBreakSeconds = 0.35f;

    public void Decorate(GameObject go)
    {
        // --- FORCE COLLIDER ---
        var existingCol = go.GetComponent<Collider>();
        if (existingCol) Destroy(existingCol); // strip bad collider
        var rend = go.GetComponentInChildren<Renderer>();
        var box = go.AddComponent<BoxCollider>();
        if (rend)
        {
            Bounds b = rend.bounds;
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = go.transform.InverseTransformVector(b.size);
        }
        box.isTrigger = false;

        // --- FORCE RIGIDBODY ---
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // --- GRAB + BREAK ---
        var grab = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()
                   ?? go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        var respawn = go.GetComponent<BreakableRespawn>();
        if (respawn) Destroy(respawn);

        var bd = go.GetComponent<BreakableDestroy>() ?? go.AddComponent<BreakableDestroy>();
        bd.floorTag = floorTag;
        bd.breakShardsPrefab = breakShardsPrefab;
        bd.breakClip = breakClip;
        bd.fxScale = breakFxScale;
        bd.breakImpulse = breakImpulse;
        bd.minSpeed = minSpeed;
        bd.style = BreakableDestroy.DestroyStyle.BreakFX;
        bd.triggerMode = BreakableDestroy.TriggerMode.OnFloorContact;

        // Give physics one frame before allowing break
        bd.enabled = false;
        StartCoroutine(EnableAfterDelay(bd, 0.1f));
    }


    IEnumerator EnableAfterDelay(BreakableDestroy bd, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bd) bd.enabled = true;
    }

    static void SafePlaceAbove(Transform t, float upOffset = 0.01f, float maxLift = 0.25f)
    {
        if (Physics.Raycast(t.position + Vector3.up * 0.5f, Vector3.down,
                            out var hit, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            t.position = hit.point + Vector3.up * upOffset;
        }

        if (!t.TryGetComponent<Collider>(out var col)) return;

        int tries = 0; float lifted = 0f;
        while (tries++ < 12)
        {
            var b = col.bounds;
            var hits = Physics.OverlapBox(b.center, b.extents * 1.02f, Quaternion.identity,
                                          ~0, QueryTriggerInteraction.Ignore);
            bool overlapping = false;
            foreach (var h in hits)
            {
                if (h.transform == t) continue;
                overlapping = true; break;
            }
            if (!overlapping) break;

            const float step = 0.02f;
            t.position += Vector3.up * step;
            lifted += step;
            if (lifted > maxLift) break;
        }
    }
}
