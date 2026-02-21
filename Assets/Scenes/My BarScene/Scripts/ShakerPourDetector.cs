using UnityEngine;

public class ShakerPourDetector : MonoBehaviour
{
    [Header("Pouring")]
    [Tooltip("Degrees from upright before pouring starts")]
    public int pourThreshold = 45;
    [Tooltip("Extra degrees to prevent flicker near the threshold")]
    public int hysteresis = 5;
    [Tooltip("Spout or mouth transform on the shaker lid")]
    public Transform origin;
    [Tooltip("Prefab that has a LineRenderer + PourStream component")]
    public GameObject streamPrefab;
    [Tooltip("ShakerContainer on the shaker body")]
    public ShakerContainer shaker;
    [Range(0f, 1f)] public float maxVol = 0.8f;

    [Header("Audio (optional)")]
    public AudioSource pourLoop;

    [Header("Rules")]
    public bool requireSealedLid = true;
    public bool requireUncapped  = true;  
    public bool requireMixed     = true;

    bool isPouring;
    PourStream current;

    void Awake()
    {
        if (pourLoop)
        {
            pourLoop.loop = true;
            pourLoop.playOnAwake = false;
            pourLoop.volume = 0f;
        }
    }

    void Update()
    {
        if (!shaker || !origin || !streamPrefab) return;

        // ----- Rules
        bool okSeal  = !requireSealedLid || shaker.IsSealed;
        bool okMixed = !requireMixed     || shaker.mixed;

        // Tilt with hysteresis
        float tilt = Vector3.Angle(transform.up, Vector3.up);
        bool tiltStart = tilt > (pourThreshold + hysteresis);
        bool tiltStop  = tilt < (pourThreshold - hysteresis);
        bool tiltOk    = isPouring ? !tiltStop : tiltStart;

        bool shouldPour = okSeal && okMixed && tiltOk;

        if (isPouring != shouldPour)
        {
            isPouring = shouldPour;
            if (isPouring) StartPour();
            else EndPour();
        }

        if (isPouring && pourLoop)
        {
            if (!pourLoop.isPlaying) pourLoop.Play();
            pourLoop.volume = maxVol;
        }
    }

    void StartPour()
    {
        var go = Instantiate(streamPrefab, origin.position, Quaternion.identity, transform);
        current = go.GetComponent<PourStream>();
        if (!current) { Debug.LogError("Stream prefab missing PourStream component."); return; }

        current.source      = origin ? origin : transform;
        current.sourceKind  = StreamSourceKind.Shaker;
        current.fromShaker  = shaker;
        current.streamColor = shaker ? shaker.MixColor() : Color.white;
        current.Begin();
    }

    void EndPour()
    {
        if (current != null) current.End();
        current = null;

        if (pourLoop && pourLoop.isPlaying)
        {
            pourLoop.Stop();
            pourLoop.volume = 0f;
        }
    }

    void OnDisable() => EndPour();
}
