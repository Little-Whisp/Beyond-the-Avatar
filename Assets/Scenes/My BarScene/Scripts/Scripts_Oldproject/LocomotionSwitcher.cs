using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class LocomotionSwitcher : MonoBehaviour
{
    public TeleportationProvider teleportationProvider;

    public ContinuousMoveProvider moveProvider;

    private void Start()
    {
        EnableTeleport();
    }

    public void EnableTeleport()
    {
        teleportationProvider.enabled = true;
        moveProvider.enabled = false;
    }

    public void EnableSmoothMove()
    {
        teleportationProvider.enabled = false;
        moveProvider.enabled = true;
    }
}
