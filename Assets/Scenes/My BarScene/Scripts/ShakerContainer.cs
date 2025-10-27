using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DefaultExecutionOrder(10)]
public class ShakerContainer : MonoBehaviour
{
    // -------- Capacity / Visuals --------
    [Header("Capacity")]
    public float capacityMl = 500f;

    [Header("Liquid visuals (shader)")]
    public Renderer liquidRenderer;
    public string fillProp = "_FillAmount";
    public string colorProp = "_BaseColor";
    public float emptyY = -0.40f, fullY = 0.20f;

    [Header("Meters (optional worldspace UI)")]
    public ShakerHUD hud;

    [Header("Colors")]
    public FlavorPalette palette;

    // -------- State for pour rules --------
    [Header("State (for pour rules)")]
    public bool IsSealed;              // lid snapped on/off (kept in sync)
    public bool IsCapped = true;       // cap snapped on/off (kept in sync)

    // -------- Audio --------
    [Header("Audio")]
    [Tooltip("AudioSource on this shaker (or a child)")]
    public AudioSource audioSource;
    [Tooltip("Loop that plays while actually shaking")]
    public AudioClip shakeLoop;
    [Tooltip("One-shot when container becomes full")]
    public AudioClip fullSound;
    [Tooltip("One-shot when mixing completes")]
    public AudioClip mixCompleteSound;

    [Range(0f,1f)] public float loopMinVol = 0.25f;
    [Range(0f,1f)] public float loopMaxVol = 1.0f;

    // -------- Mixing --------
    [Header("Mixing")]
    public bool mixed;                 // true after enough shake energy
    public float mixProgress;          // 0..1
    public float mixNeeded = 1.0f;     // energy required

    // -------- XR Motion Inputs --------
    [Header("XR Motion (optional)")]
    public XRSocketInteractor lidSocket;       // lid socket on shaker
    public XRSocketInteractor capSocket;       // cap socket on spout
    public XRGrabInteractable cupGrab;         // grabbing the shaker

    [Tooltip("Delay after lid attach before shakes count")]
    public float armDelayAfterAttach = 0.35f;

    [Header("Shake Detection Tuning")]
    public float speedThreshold = 0.8f;        // m/s
    public float accelThreshold = 3f;          // m/s^2
    public float angularThreshold = 120f;      // deg/s
    public float angularWeight = 0.10f;
    public float speedDeadzone = 0.05f;
    public float angDeadzone = 5f;
    public float gainPerShake = 0.40f;         // energy per valid shake
    public float cooldown = 0.12f;             // sec between energy grants

    [Header("Loop Smoothing")]
    public float silenceToStop = 0.25f;        // idle time before stopping
    public float minLoopHold = 0.30f;          // minimum time the loop stays on once started

    // volumes (ml)
    public float mlOld, mlLife, mlImp;

    // Events
    [Header("Events (optional)")]
    public UnityEvent onMixStart;
    public UnityEvent onMixStop;

    // -------- Internals --------
    Material mat;
    int fillID, colorID;
    public float TotalMl => mlOld + mlLife + mlImp;
    public bool HasLiquid => TotalMl > 0.01f;
    public bool IsFull => TotalMl >= capacityMl - 0.01f;
    public bool CanMix => IsSealed && IsCapped;

    // motion state
    bool isHeld, lidOn, capOn, isMixing, loopPlaying;
    float armUntil, nextEnergyTime, noShakeTimer, loopHoldUntil;
    Vector3 lastPos, lastVel;
    Quaternion lastRot;

    void Awake()
    {
        // Visuals
        if (liquidRenderer) mat = liquidRenderer.material;
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProp) ? "_FillAmount" : fillProp);
        colorID = Shader.PropertyToID(string.IsNullOrWhiteSpace(colorProp) ? "_BaseColor" : colorProp);
        ApplyVisuals();

        // XR hookups
        if (!cupGrab)
            cupGrab = GetComponent<XRGrabInteractable>() ??
                      GetComponentInChildren<XRGrabInteractable>(true) ??
                      GetComponentInParent<XRGrabInteractable>();
        if (cupGrab)
        {
            cupGrab.selectEntered.AddListener(OnHeld);
            cupGrab.selectExited.AddListener(OnReleased);
        }

        if (lidSocket)
        {
            lidSocket.selectEntered.AddListener(OnLidAttached);
            lidSocket.selectExited.AddListener(OnLidDetached);
            lidOn = lidSocket.hasSelection;
            IsSealed = lidOn;
        }
        else
        {
            // if no socket assigned, assume current inspector value
            lidOn = IsSealed;
        }

        if (capSocket)
        {
            capSocket.selectEntered.AddListener(OnCapAttached);
            capSocket.selectExited.AddListener(OnCapDetached);
            capOn = capSocket.hasSelection;
            IsCapped = capOn;
        }
        else
        {
            capOn = IsCapped; // honor inspector if no socket provided
        }

        // Audio defaults
        if (audioSource)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }

        ResetMotionFrame();
        StopLoop(true);
    }

    void OnDestroy()
    {
        if (cupGrab)
        {
            cupGrab.selectEntered.RemoveListener(OnHeld);
            cupGrab.selectExited.RemoveListener(OnReleased);
        }
        if (lidSocket)
        {
            lidSocket.selectEntered.RemoveListener(OnLidAttached);
            lidSocket.selectExited.RemoveListener(OnLidDetached);
        }
        if (capSocket)
        {
            capSocket.selectEntered.RemoveListener(OnCapAttached);
            capSocket.selectExited.RemoveListener(OnCapDetached);
        }
    }

    // -------------------- XR callbacks --------------------
    void OnHeld(SelectEnterEventArgs _) { isHeld = true; ResetMotionFrame(); }
    void OnReleased(SelectExitEventArgs _) { isHeld = false; AddMixEnergy(0f); SetMixing(false); StopLoop(true); }

    void OnLidAttached(SelectEnterEventArgs _) { lidOn = true;  IsSealed = true;  armUntil = Time.time + armDelayAfterAttach; ResetMotionFrame(); }
    void OnLidDetached(SelectExitEventArgs _) { lidOn = false; IsSealed = false; AddMixEnergy(0f); SetMixing(false); StopLoop(true); }

    void OnCapAttached(SelectEnterEventArgs _) { capOn = true;  IsCapped = true; }
    void OnCapDetached(SelectExitEventArgs _) { capOn = false; IsCapped = false; AddMixEnergy(0f); StopLoop(true); }

    // -------------------- MAIN UPDATE --------------------
    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // gates (must be true to mix & to play loop)
        bool gatedOut = !isHeld || !lidOn || !capOn || Time.time < armUntil || !HasLiquid || mixed || !CanMix;
        if (gatedOut)
        {
            // not actively mixing -> no loop (respect min hold)
            TryStopLoopSoft();
            AddMixEnergy(0f);
            ResetMotionFrame();
            return;
        }

        // motion deltas
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        Vector3 vel = (pos - lastPos) / dt;
        float linSpeed = vel.magnitude;
        float angRateDegPerSec = Quaternion.Angle(lastRot, rot) / dt;

        bool moving = !(linSpeed < speedDeadzone && angRateDegPerSec < angDeadzone);
        Vector3 acc = (vel - lastVel) / dt;
        bool directionFlip = Vector3.Dot(vel, lastVel) < -0.2f;
        float combinedSpeed = linSpeed + angRateDegPerSec * angularWeight;

        bool isShake =
            moving &&
            combinedSpeed > speedThreshold &&
            (directionFlip || acc.magnitude > accelThreshold || angRateDegPerSec > angularThreshold);

        // only while ACTUALLY mixing (not done yet)
        bool isActiveMix = isShake && !mixed && HasLiquid && CanMix;

        if (isActiveMix)
        {
            SetMixing(true);

            float intensity01 = Mathf.Clamp01((combinedSpeed - speedThreshold) / (speedThreshold * 2f));
            StartLoop(intensity01);

            // grant energy on cooldown ticks
            if (Time.time >= nextEnergyTime)
            {
                AddMixEnergy(gainPerShake);
                nextEnergyTime = Time.time + cooldown;
            }
            else
            {
                // keep audio "alive" this frame w/o extra energy
                AddMixEnergy(0.0001f);
            }

            noShakeTimer = 0f;
        }
        else
        {
            // not actively mixing -> no loop (respect min hold)
            TryStopLoopSoft();
            AddMixEnergy(0f);

            noShakeTimer += dt;
            if (noShakeTimer >= silenceToStop) SetMixing(false);
        }

        lastPos = pos;
        lastRot = rot;
        lastVel = vel;
    }

    void ResetMotionFrame()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastVel = Vector3.zero;
        nextEnergyTime = 0f;
        noShakeTimer = 0f;
    }

    // -------------------- Mixing & Audio core --------------------
    public void AddMixEnergy(float amount)
    {
        // if empty or already finished, make sure loop is off and bail
        if (TotalMl <= 0f || mixed)
        {
            StopLoop(true);
            return;
        }

        bool wasMixed = mixed;

        mixProgress += Mathf.Max(0f, amount);
        float p = Mathf.Clamp01(mixProgress / Mathf.Max(0.0001f, mixNeeded));
        if (p >= 1f) mixed = true;

        hud?.SetMixed(mixed, p);
        ApplyVisuals();

        // if we JUST finished: hard-stop loop and ping once
        if (!wasMixed && mixed)
        {
            StopLoop(true);
            if (audioSource && mixCompleteSound) audioSource.PlayOneShot(mixCompleteSound);
            SetMixing(false);
            return;
        }
    }

    void SetMixing(bool v)
    {
        if (isMixing == v) return;
        isMixing = v;
        if (isMixing) onMixStart?.Invoke(); else onMixStop?.Invoke();
    }

    void StartLoop(float intensity01)
    {
        if (!audioSource || !shakeLoop) return;
        if (!loopPlaying)
        {
            audioSource.clip = shakeLoop;
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.Play();
            loopPlaying = true;
            loopHoldUntil = Time.time + Mathf.Max(0f, minLoopHold); // NEW: latch on
        }
        audioSource.volume = Mathf.Lerp(loopMinVol, loopMaxVol, intensity01);
    }

    // stop immediately (used on release, lid/cap off, finished, clear)
    void StopLoop(bool immediate)
    {
        if (!audioSource || !loopPlaying) return;
        if (immediate)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
            loopPlaying = false;
            return;
        }
        StartCoroutine(FadeOut(0.10f));
    }

    // soft stop that respects minLoopHold
    void TryStopLoopSoft()
    {
        if (!loopPlaying) return;
        if (Time.time < loopHoldUntil) return; // NEW: keep playing until min hold reached
        // after hold window, fade out
        StartCoroutine(FadeOut(0.10f));
    }

    System.Collections.IEnumerator FadeOut(float dur)
    {
        if (!audioSource) yield break;
        float start = audioSource.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, dur);
            audioSource.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
        loopPlaying = false;
    }

    // -------------------- Liquid logic --------------------
    public void AddFlavor(Flavor f, float ml)
    {
        if (ml <= 0f || IsFull) return;

        bool wasFullBefore = IsFull;

        float room = Mathf.Max(0, capacityMl - TotalMl);
        ml = Mathf.Min(ml, room);
        if (ml <= 0f) return;

        switch (f)
        {
            case Flavor.OldTwitter: mlOld += ml; break;
            case Flavor.LifeJacket: mlLife += ml; break;
            default: mlImp += ml; break;
        }

        mixed = false;
        mixProgress = 0f;

        ApplyVisuals();
        hud?.SetValues(mlOld, mlLife, mlImp, capacityMl);
        hud?.SetMixed(false, 0f);

        if (!wasFullBefore && IsFull && audioSource && fullSound)
            audioSource.PlayOneShot(fullSound);
    }

    public float Drain(float ml)
    {
        float available = Mathf.Max(0f, TotalMl);
        if (available <= 0f || ml <= 0f) return 0f;

        float take = Mathf.Min(ml, available);

        float t = Mathf.Max(0.0001f, available);
        float po = mlOld / t;
        float pl = mlLife / t;
        float pi = mlImp / t;

        mlOld = Mathf.Max(0f, mlOld - take * po);
        mlLife = Mathf.Max(0f, mlLife - take * pl);
        mlImp = Mathf.Max(0f, mlImp - take * pi);

        ApplyVisuals();
        hud?.SetValues(mlOld, mlLife, mlImp, capacityMl);

        if (!HasLiquid) StopLoop(true); // if emptied, kill the loop

        return take;
    }

    public (float o, float l, float i) Percentages()
    {
        float t = Mathf.Max(0.0001f, TotalMl);
        return (mlOld / t, mlLife / t, mlImp / t);
    }

    public Color MixColor()
    {
        var (po, pl, pi) = Percentages();
        Color co = palette ? palette.oldTwitter : Color.cyan;
        Color cl = palette ? palette.lifeJacket : Color.yellow;
        Color ci = palette ? palette.imposter : Color.magenta;
        return co * po + cl * pl + ci * pi;
    }

    void ApplyVisuals()
    {
        if (!mat) return;
        float t = Mathf.Clamp01(TotalMl / Mathf.Max(0.0001f, capacityMl));
        float y = Mathf.Lerp(emptyY, fullY, t);
        mat.SetVector(fillID, new Vector3(0, y, 0));
        if (mixed) mat.SetColor(colorID, MixColor());
    }

    public void Clear()
    {
        mlOld = mlLife = mlImp = 0f;
        mixed = false; mixProgress = 0f;
        ApplyVisuals();
        hud?.SetValues(0, 0, 0, capacityMl);
        hud?.SetMixed(false, 0);
        StopLoop(true); // safety
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!liquidRenderer) return;
        fillID = Shader.PropertyToID(string.IsNullOrWhiteSpace(fillProp) ? "_FillAmount" : fillProp);
        colorID = Shader.PropertyToID(string.IsNullOrWhiteSpace(colorProp) ? "_BaseColor" : colorProp);

        var m = Application.isPlaying ? liquidRenderer.material : liquidRenderer.sharedMaterial;
        if (!m) return;

        float t = Mathf.Clamp01(TotalMl / Mathf.Max(0.0001f, capacityMl));
        float y = Mathf.Lerp(emptyY, fullY, t);
        m.SetVector(fillID, new Vector3(0, y, 0));
    }
#endif
}
