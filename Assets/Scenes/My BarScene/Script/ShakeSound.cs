using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShakeSound : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource audioSource;          // your shaker clip
    public XRSocketInteractor lidSocket;     // Snap Point socket
    public XRGrabInteractable cupGrab;       // cup's XR Grab (optional)

    [Header("Tuning")]
    public float speedThreshold = 1.3f;      // m/s of movement
    public float accelThreshold = 6f;        
    public float angularThreshold = 180f;    // deg/s spin burst
    public float angularWeight = 0.10f;      // small bonus from spin
    public float cooldown = 0.18f;           // min time between sounds
    public float armDelayAfterAttach = 0.35f;
    public bool requireHeld = true;          // only when player is holding

    Vector3 lastPos, lastVel;
    Quaternion lastRot;
    float nextTime, armUntil;
    bool lidOn;

    void Awake()
    {
        if (!cupGrab) cupGrab = GetComponent<XRGrabInteractable>();
        if (lidSocket)
        {
            lidSocket.selectEntered.AddListener(_ => { lidOn = true;  armUntil = Time.time + armDelayAfterAttach; ResetDeltas(); });
            lidSocket.selectExited .AddListener(_ => { lidOn = false; });
            lidOn = lidSocket.hasSelection;
        }
    }

    void OnEnable() { ResetDeltas(); }
    void OnDisable()
    {
        if (lidSocket)
        {
            lidSocket.selectEntered.RemoveAllListeners();
            lidSocket.selectExited .RemoveAllListeners();
        }
    }

    void ResetDeltas()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastVel = Vector3.zero;
        nextTime = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // gates: lid attached, armed, (optionally) being held
        if (!lidOn || Time.time < armUntil) { ResetFrame(dt); return; }
        if (requireHeld && cupGrab && !cupGrab.isSelected) { ResetFrame(dt); return; }

        // transform-delta motion (works with Instantaneous / Kinematic)
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        Vector3 vel = (pos - lastPos) / dt;                     // m/s
        float speed = vel.magnitude + Quaternion.Angle(lastRot, rot) / dt * angularWeight;
        Vector3 acc = (vel - lastVel) / dt;                     // m/s²
        float angDegPerSec = Quaternion.Angle(lastRot, rot) / dt;

        bool directionFlip = Vector3.Dot(vel, lastVel) < -0.2f; // ~>100° reversal

        bool isShake =
            speed > speedThreshold &&
            (directionFlip || acc.magnitude > accelThreshold || angDegPerSec > angularThreshold);

        if (isShake && Time.time >= nextTime)
        {
            if (audioSource && audioSource.clip)
                audioSource.PlayOneShot(audioSource.clip);
            nextTime = Time.time + cooldown;
        }

        // keep history
        lastPos = pos;
        lastRot = rot;
        lastVel = vel;
    }

    void ResetFrame(float dt)
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastVel = Vector3.zero;
    }
}
