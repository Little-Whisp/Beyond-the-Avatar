using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PourDetector : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Angle past which the bottle starts pouring (degrees)")]
    public int pourThreshold = 45;               // starts pouring past this angle
    public Transform origin;                     // spout/mouth of bottle
    public GameObject streamPrefab;

    [Header("Audio")]
    [Tooltip("AudioSource located at (or parented under) the bottle spout")]
    public AudioSource pourSource;               // loop source for continuous pour
    public AudioClip pourStartSfx;               // 'glug' on start
    public AudioClip pourEndSfx;                 // 'drip stop' on end
    [Range(0f, 1f)] public float maxVolume = 0.8f;
    public float minPitch = 0.95f;
    public float maxPitch = 1.25f;
    public float fadeOutTime = 0.12f;            // quick fade on stop

    private bool isPouring = false;
    private PourStream currentStream = null;
    private Coroutine fadeCo;

    void Update()
    {
        bool shouldPour = CalculateTiltAngle() > pourThreshold;

        if (isPouring != shouldPour)
        {
            isPouring = shouldPour;

            if (isPouring) StartPour();
            else EndPour();
        }

        // While pouring, drive volume/pitch by tilt strength (0..1)
        if (isPouring && pourSource != null)
        {
            float t = Tilt01(); // 0 at threshold, 1 at ~fully inverted
            pourSource.volume = maxVolume * t;
            pourSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }
    }

    private void StartPour()
    {
        // stream
        currentStream = CreateStream();
        currentStream.Begin();

        // audio
        if (pourSource != null)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);

            if (pourStartSfx != null) pourSource.PlayOneShot(pourStartSfx);

            // ensure loop is running
            if (!pourSource.isPlaying)
            {
                pourSource.loop = true;
                pourSource.Play();
            }
            // jump to audible volume immediately based on current tilt
            float t = Tilt01();
            pourSource.volume = maxVolume * t;
            pourSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }
    }

    private void EndPour()
    {
        // stream
        if (currentStream != null)
        {
            currentStream.End();
            currentStream = null;
        }

        // audio
        if (pourSource != null)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeOutAndStop(pourSource, fadeOutTime));
            if (pourEndSfx != null) AudioSource.PlayClipAtPoint(pourEndSfx, origin != null ? origin.position : transform.position, 0.7f);
        }
    }

    // 0° = upright, 180° = upside down
    private float CalculateTiltAngle()
    {
        return Vector3.Angle(transform.up, Vector3.up);
    }

    // Map tilt to 0..1 (0 at threshold, ~1 near fully upside-down)
    private float Tilt01()
    {
        float ang = CalculateTiltAngle();
        return Mathf.Clamp01(Mathf.InverseLerp(pourThreshold, 120f, ang));
    }

   private PourStream CreateStream()
{
    var go = Instantiate(streamPrefab, origin.position, Quaternion.identity, transform);
    var s = go.GetComponent<PourStream>();
    s.source     = origin ? origin : transform;
    s.sourceKind = StreamSourceKind.Bottle;
    s.fromBottle = GetComponent<BottleFlavor>();

    // Optional tint:
    var pal = FindAnyObjectByType<FlavorPalette>();
    if (pal && s.fromBottle)
    {
        s.streamColor = s.fromBottle.flavor switch
        {
            Flavor.OldTwitter => pal.oldTwitter,
            Flavor.LifeJacket => pal.lifeJacket,
            _                 => pal.imposter
        };
    }
    return s;
}

    private IEnumerator FadeOutAndStop(AudioSource src, float time)
    {
        if (time <= 0f) { src.Stop(); yield break; }
        float startVol = src.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            src.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        src.Stop();
        src.volume = startVol; // restore for next start
    }

    void OnDisable()
    {
        // ensure audio/stream are cleaned if object is disabled
        if (currentStream != null) { currentStream.End(); currentStream = null; }
        if (pourSource != null && pourSource.isPlaying) pourSource.Stop();
    }
}
