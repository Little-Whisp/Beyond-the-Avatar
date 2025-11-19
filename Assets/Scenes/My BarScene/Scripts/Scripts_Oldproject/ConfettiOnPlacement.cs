using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfettiOnPlacement : MonoBehaviour
{
    public GameObject confettiEffect; // The confetti particle system prefab
    public AudioClip confettiSound;   // Sound to play when confetti triggers
    private AudioSource audioSource;  // Audio source to play the sound

    private void Start()
    {
        // Get or add an AudioSource component
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void TriggerConfetti()
    {
        Debug.Log("Confetti and Sound should be triggered!");

        // Prevent error if confettiEffect is not assigned
        if (confettiEffect == null)
        {
            Debug.LogError("ConfettiOnPlacement: confettiEffect prefab is not assigned in the inspector!");
            return;
        }

        // Spawn Confetti slightly above
        Vector3 spawnPosition = transform.position + new Vector3(0, 1, 0);
        GameObject confettiInstance = Instantiate(confettiEffect, spawnPosition, Quaternion.identity);

        // Play Particle System
        ParticleSystem ps = confettiInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        else
        {
            Debug.LogError("Confetti prefab is missing a ParticleSystem!");
        }

        // Play Sound Effect
        if (confettiSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(confettiSound, 0.3f); // Adjust volume (0.0 - 1.0)
        }
        else
        {
            Debug.LogError("Missing AudioClip or AudioSource!");
        }
    }
}
