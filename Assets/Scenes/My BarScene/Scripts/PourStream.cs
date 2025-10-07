using System.Collections;
using UnityEngine;

public enum StreamSourceKind { Bottle, Shaker }

public class PourStream : MonoBehaviour
{
    [Header("Flow")]
    public StreamSourceKind sourceKind = StreamSourceKind.Bottle;
    public float flowMlPerSec = 120f;
    public float maxDistance = 2.0f;

    [Header("Visuals")]
    public LineRenderer lineRenderer;
    public Color streamColor = Color.white;

    [Header("Refs set by creator")]
    public Transform source;            // spout
    public BottleFlavor fromBottle;     // if Bottle
    public ShakerContainer fromShaker;  // if Shaker

    Coroutine co;

    void Awake()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer && lineRenderer.positionCount < 2) lineRenderer.positionCount = 2;
    }

    void OnEnable()
    {
        if (lineRenderer)
        {
            lineRenderer.startColor = streamColor;
            lineRenderer.endColor   = streamColor;
        }
    }

    public void Begin()
    {
        if (co == null) co = StartCoroutine(PourLoop());
    }

    public void End()
    {
        if (co != null) StopCoroutine(co);
        co = null;
        Destroy(gameObject);
    }

    IEnumerator PourLoop()
    {
        while (true)
        {
            Vector3 start = source ? source.position : transform.position;
            Vector3 end = FindEndPoint(start);

            if (lineRenderer)
            {
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }

            FeedTarget(start);
            yield return null;
        }
    }

    Vector3 FindEndPoint(Vector3 start)
    {
        Ray ray = new Ray(start, Vector3.down);
        if (Physics.Raycast(ray, out var hit, maxDistance)) return hit.point;
        return ray.GetPoint(maxDistance);
    }

    void FeedTarget(Vector3 startPoint)
    {
        if (!Physics.Raycast(new Ray(startPoint, Vector3.down), out var hit, maxDistance))
            return;

        float ml = flowMlPerSec * Time.deltaTime;

        // BOTTLE → only Shaker
        if (sourceKind == StreamSourceKind.Bottle)
        {
            var shaker = hit.collider.GetComponentInParent<ShakerContainer>();
            if (shaker != null && fromBottle != null)
                shaker.AddFlavor(fromBottle.flavor, ml);
            return;
        }

        // SHAKER → only Glass (+ drain shaker)
        if (sourceKind == StreamSourceKind.Shaker)
        {
            if (!fromShaker || fromShaker.TotalMl <= 0f) return;

            var glass = hit.collider.GetComponentInParent<GlassFill>();
            if (glass == null || glass.IsFull) return;

            float glassRoom = Mathf.Max(0f, glass.capacityMl - glass.currentMl);
            float take = Mathf.Min(ml, glassRoom, fromShaker.TotalMl);
            if (take <= 0f) return;

            glass.AddLiquid(take);

            var receiver = glass.GetComponent<GlassResultReceiver>();
            if (receiver) receiver.Receive(fromShaker, take);

            fromShaker.Drain(take);
        }
    }
}
