using UnityEngine;
using Unity.XR.CoreUtils;

public class MiniGameJoinArea : MonoBehaviour
{
    [Header("Clamp Options")]
    public bool clampY = false;
    [SerializeField] float _margin = 0.05f;   // small inset to avoid jitter

    XROrigin _origin;
    Bounds _bounds;
    bool _areaSet;

    void Awake()
    {
        _origin = GetComponent<XROrigin>();
        enabled = false; // the binder enables us
    }

    // Called by binder
    public void SetArea(BoxCollider area)
    {
        _bounds = area.bounds;
        _areaSet = true;
    }

    // Called by binder (center/size variant)
    public void SetArea(Vector3 center, Vector3 size)
    {
        _bounds = new Bounds(center, size);
        _areaSet = true;
    }

    void OnEnable()  { Application.onBeforeRender += Clamp; }
    void OnDisable() { Application.onBeforeRender -= Clamp; }
    void LateUpdate(){ Clamp(); } // catch normal frame

    void Clamp()
    {
        if (!_areaSet || !_origin) return;
        var cam = _origin.Camera;
        if (!cam) return;

        Vector3 camWorld = cam.transform.position;

        // world-space limits with a tiny margin
        var min = _bounds.min + Vector3.one * _margin;
        var max = _bounds.max - Vector3.one * _margin;

        // clamp the camera/head
        Vector3 clamped = new Vector3(
            Mathf.Clamp(camWorld.x, min.x, max.x),
            clampY ? Mathf.Clamp(camWorld.y, min.y, max.y) : camWorld.y,
            Mathf.Clamp(camWorld.z, min.z, max.z)
        );

        // if outside, move rig so head ends up clamped
        if ((clamped - camWorld).sqrMagnitude > 0.000001f)
            _origin.MoveCameraToWorldLocation(clamped);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_areaSet) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_bounds.center, _bounds.size);
    }
#endif
}
