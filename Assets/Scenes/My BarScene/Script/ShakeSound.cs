using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class ShakeSound : MonoBehaviour
{
    public AudioSource shakeSound;
    public float shakeThreshold = 2.0f; // How much speed change counts as a "shake"
    public float cooldown = 0.2f;       // Minimum time between sounds

    private Rigidbody rb;
    private Vector3 lastVelocity;
    private float lastSoundTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastVelocity = rb.linearVelocity;
    }

    void Update()
    {
        Vector3 velocityChange = rb.linearVelocity - lastVelocity;

        // Check if the object was "shaken" (big velocity change)
        if (velocityChange.magnitude > shakeThreshold)
        {
            if (Time.time - lastSoundTime > cooldown)
            {
                shakeSound.Play();
                lastSoundTime = Time.time;
            }
        }

        lastVelocity = rb.linearVelocity;
    }
}
