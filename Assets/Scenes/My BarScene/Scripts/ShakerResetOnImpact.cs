using UnityEngine;

public class ShakerResetOnFloor : MonoBehaviour
{
    public ShakerContainer shaker;

    [Header("Floor detection (tag-only)")]
    public string floorTag = "Floor";   // must match the tag on your floor collider(s)

    void Reset()
    {
        if (!shaker)
            shaker = GetComponent<ShakerContainer>() ??
                     GetComponentInChildren<ShakerContainer>(true);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!shaker) return;

        // Only react to colliders with the Floor tag (no speed/impulse checks)
        if (c.collider.CompareTag(floorTag))
        {
            // Empty the shaker + reset states so player can start over
            shaker.Clear();
            shaker.IsSealed = false;   // lid considered off after a fall
            shaker.IsCapped = true;    // cap back on by default (optional)
        }
    }
}
