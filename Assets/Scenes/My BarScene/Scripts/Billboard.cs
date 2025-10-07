using UnityEngine;
public class Billboard : MonoBehaviour
{
    public bool lockY = true;
    void LateUpdate()
    {
        var cam = Camera.main; if (!cam) return;
        var fwd = cam.transform.forward;
        if (lockY) fwd.y = 0f;
        transform.forward = fwd.normalized;
    }
}
