using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(10)]
public class ShakerMixController : MonoBehaviour
{
    public ShakerContainer container;
    public Rigidbody rb;

    [Header("Shake detection")]
    public float linearSpeed = 1.2f;   // m/s threshold
    public float angularSpeed = 3f;    // rad/s threshold
    public float sustain = 0.5f;       // keep mixing for this long after last shake
    public float energyPerMeter = 0.25f; // energy gain per m/s per FixedUpdate

    [Header("Events (optional)")]
    public UnityEvent onMixStart;
    public UnityEvent onMixStop;

    public bool IsShaking   { get; private set; }
    public bool IsMixing    { get; private set; }

    float tail;

    void Reset(){ rb = GetComponent<Rigidbody>(); container = GetComponent<ShakerContainer>(); }

    void FixedUpdate()
    {
        if (!rb || !container) return;

        // Only allowed to mix when sealed 
        if (!container.CanMix) { SetMixing(false); return; }

        IsShaking = rb.linearVelocity.magnitude > linearSpeed ||
                    rb.angularVelocity.magnitude > angularSpeed;

        if (IsShaking) tail = sustain; else tail = Mathf.Max(0f, tail - Time.fixedDeltaTime);

        bool willMix = tail > 0f;
        SetMixing(willMix);

        if (IsMixing && container.HasLiquid)
        {
            // Add some mix energy based on how hard we’re moving
            float energy = rb.linearVelocity.magnitude * energyPerMeter * Time.fixedDeltaTime;
            container.AddMixEnergy(energy);
        }
    }

    void SetMixing(bool v)
    {
        if (IsMixing == v) return;
        IsMixing = v;
        if (IsMixing) onMixStart?.Invoke(); else onMixStop?.Invoke();
    }
}
