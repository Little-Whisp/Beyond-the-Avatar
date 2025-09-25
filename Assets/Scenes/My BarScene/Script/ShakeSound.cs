using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShakeSound : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource audioSource;                 // assign a loopable shaker clip here
    public XRSocketInteractor lidSocket;
    public XRGrabInteractable cupGrab;
    public ShakerContainer shaker;

    [Header("Mix")]
    public float mixGainPerShake = 0.40f;

    [Header("Tuning")]
    public float speedThreshold = 0.8f;
    public float accelThreshold = 3f;
    public float angularThreshold = 120f;
    public float angularWeight = 0.10f;
    public float cooldown = 0.12f;                 // still used for mix pulses
    public float armDelayAfterAttach = 0.35f;
    public float speedDeadzone = 0.05f;
    public float angDeadzone = 5f;

    [Header("Audio Loop")]
    public float silenceToStop = 0.25f;            // how long w/o shakes before we stop
    public float fadeOutTime = 0.10f;              // quick fade when stopping
    public float minVol = 0.25f;                   // volume scales with shake intensity
    public float maxVol = 1.00f;

    Vector3 lastPos, lastVel;
    Quaternion lastRot;
    float nextTime, armUntil, noShakeTimer;
    bool lidOn, isHeld, loopPlaying;
    Coroutine fadeCo;

    void Awake()
    {
        if (!cupGrab)
            cupGrab = GetComponent<XRGrabInteractable>() ??
                      GetComponentInChildren<XRGrabInteractable>(true) ??
                      GetComponentInParent<XRGrabInteractable>();

        if (!shaker)
            shaker = GetComponent<ShakerContainer>() ??
                     GetComponentInChildren<ShakerContainer>(true) ??
                     GetComponentInParent<ShakerContainer>();

        if (cupGrab)
        {
            cupGrab.selectEntered.AddListener(OnHeld);
            cupGrab.selectExited .AddListener(OnReleased);
        }

        if (lidSocket)
        {
            lidSocket.selectEntered.AddListener(OnLidAttached);
            lidSocket.selectExited .AddListener(OnLidDetached);
            lidOn = lidSocket.hasSelection;
        }

        if (audioSource) { audioSource.loop = true; audioSource.playOnAwake = false; audioSource.volume = 0f; }
    }

    void OnDestroy()
    {
        if (cupGrab)
        {
            cupGrab.selectEntered.RemoveListener(OnHeld);
            cupGrab.selectExited .RemoveListener(OnReleased);
        }
        if (lidSocket)
        {
            lidSocket.selectEntered.RemoveListener(OnLidAttached);
            lidSocket.selectExited .RemoveListener(OnLidDetached);
        }
    }

    void OnEnable()  { ResetDeltas(); }
    void OnHeld(SelectEnterEventArgs _)    { isHeld = true;  ResetDeltas(); }
    void OnReleased(SelectExitEventArgs _) { isHeld = false; StopLoop(true); }
    void OnLidAttached(SelectEnterEventArgs _) { lidOn = true; armUntil = Time.time + armDelayAfterAttach; ResetDeltas(); }
    void OnLidDetached(SelectExitEventArgs _)  { lidOn = false; StopLoop(true); }

    void ResetDeltas()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastVel = Vector3.zero;
        nextTime = 0f;
        noShakeTimer = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // gates: must be held & lid on & armed
        if (!lidOn || Time.time < armUntil || !isHeld)
        {
            ResetFrame();
            StopLoop(false);
            return;
        }

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        Vector3 vel = (pos - lastPos) / dt;
        float linSpeed = vel.magnitude;
        float angRate  = Quaternion.Angle(lastRot, rot) / dt;

        // deadzone
        bool moving = !(linSpeed < speedDeadzone && angRate < angDeadzone);

        Vector3 acc = (vel - lastVel) / dt;
        bool directionFlip = Vector3.Dot(vel, lastVel) < -0.2f;
        float combinedSpeed = linSpeed + angRate * angularWeight;

        bool isShake =
            moving &&
            combinedSpeed > speedThreshold &&
            (directionFlip || acc.magnitude > accelThreshold || angRate > angularThreshold);

        // audio: start/maintain loop while shaking; fade out after a pause
        if (isShake)
        {
            float intensity01 = Mathf.Clamp01((combinedSpeed - speedThreshold) / (speedThreshold * 2f));
            StartLoop(intensity01);
            noShakeTimer = 0f;

            // mix progress “ticks”
            if (Time.time >= nextTime)
            {
                if (shaker) shaker.AddMixEnergy(mixGainPerShake);
                nextTime = Time.time + cooldown;
            }
        }
        else
        {
            noShakeTimer += dt;
            if (noShakeTimer >= silenceToStop) StopLoop(false);
        }

        lastPos = pos; lastRot = rot; lastVel = vel;
    }

    void ResetFrame()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastVel = Vector3.zero;
    }

    void StartLoop(float intensity01)
    {
        if (!audioSource) return;

        if (fadeCo != null) { StopCoroutine(fadeCo); fadeCo = null; }
        if (!loopPlaying) { audioSource.volume = 0f; audioSource.Play(); loopPlaying = true; }

        audioSource.volume = Mathf.Lerp(minVol, maxVol, intensity01);
    }

    void StopLoop(bool immediate)
    {
        if (!audioSource || !loopPlaying) return;

        if (immediate || fadeOutTime <= 0f)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            audioSource.Stop();
            audioSource.volume = 0f;
            loopPlaying = false;
            return;
        }

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutTime;
            audioSource.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
        loopPlaying = false;
        fadeCo = null;
    }
}
