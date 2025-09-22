using System.Collections;
using UnityEngine;

public class Stream : MonoBehaviour
{
    [Header("Flow Settings")]
    [Tooltip("How fast liquid is added to the glass (ml per second)")]
    public float flowMlPerSec = 120f;
    public float maxDistance = 2.0f; // how far down we check for a hit

    [Header("References")]
    public Transform source;          // spout of the bottle
    private LineRenderer lineRenderer;
    private Coroutine pouringRoutine;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        // Initialize both line points at the start position
        Vector3 startPos = source != null ? source.position : transform.position;
        MoveToPosition(0, startPos);
        MoveToPosition(1, startPos);
    }

    public void Begin()
    {
        if (pouringRoutine == null)
            pouringRoutine = StartCoroutine(PourLoop());
    }

    public void End()
    {
        if (pouringRoutine != null)
            StopCoroutine(pouringRoutine);

        pouringRoutine = null;
        Destroy(gameObject); // cleanup stream object
    }

    private IEnumerator PourLoop()
    {
        while (true)
        {
            Vector3 start = source != null ? source.position : transform.position;
            Vector3 end = FindEndPoint(start);

            MoveToPosition(0, start);
            MoveToPosition(1, end);

            FeedGlass(start);

            yield return null; // wait one frame
        }
    }

    private Vector3 FindEndPoint(Vector3 start)
    {
        Ray ray = new Ray(start, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return hit.point;

        return ray.GetPoint(maxDistance);
    }

    private void FeedGlass(Vector3 startPoint)
    {
        // Raycast down from spout to check if we hit a glass
        if (Physics.Raycast(new Ray(startPoint, Vector3.down), out RaycastHit hit, maxDistance))
        {
            GlassFill glass = hit.collider.GetComponentInParent<GlassFill>();
            if (glass != null && !glass.IsFull)
            {
                float ml = flowMlPerSec * Time.deltaTime;
                // trim last bit so we don't overshoot
                ml = Mathf.Min(ml, glass.capacityMl - glass.currentMl);
                glass.AddLiquid(ml);
            }

        }
    }

    private void MoveToPosition(int index, Vector3 targetPosition)
    {
        lineRenderer.SetPosition(index, targetPosition);
    }
}
