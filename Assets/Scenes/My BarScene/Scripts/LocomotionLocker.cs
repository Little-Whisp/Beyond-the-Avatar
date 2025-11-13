using UnityEngine;

public class LocomotionLocker : MonoBehaviour
{
    [Tooltip("Parent object that contains Turn, Move, Teleportation, etc.")]
    public GameObject locomotionRoot;

    [Tooltip("If true, locomotion is disabled when the scene starts (lobby).")]
    public bool lockedAtStart = true;

    void Start()
    {
        SetLocked(lockedAtStart);
    }

    public void SetLocked(bool locked)
    {
        if (locomotionRoot != null)
            locomotionRoot.SetActive(!locked);
    }

    public void Lock()   => SetLocked(true);
    public void Unlock() => SetLocked(false);
}
