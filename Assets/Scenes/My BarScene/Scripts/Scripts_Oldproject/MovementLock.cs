using UnityEngine;

public class MovementLock : MonoBehaviour
{
    public Behaviour[] movementScripts;

    public void SetMovementEnabled(bool enabled)
    {
        foreach (var script in movementScripts)
        {
            script.enabled = enabled;
        }
    }
}
