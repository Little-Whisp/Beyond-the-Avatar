using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Gizmo (editor only)")]
    public float radius = 0.25f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
    }
}
