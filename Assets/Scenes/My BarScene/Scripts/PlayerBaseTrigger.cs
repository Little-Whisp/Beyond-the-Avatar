using UnityEngine;

public class PlayerBaseTrigger : MonoBehaviour
{
    public string playerTag = "Player";       // VR player rig tag
    public string assignedPlayerName;         // e.g. "Player 1"
    public string assignedAvatarType;         // e.g. "AvatarGreen"

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        // Get the PlayerIdentity component from the player
        PlayerIdentity identity = other.GetComponent<PlayerIdentity>();
        if (identity != null)
        {
            assignedPlayerName = identity.playerName;
            assignedAvatarType = identity.avatarType;

            Debug.Log($"[PlayerBaseTrigger] {assignedPlayerName} with avatar {assignedAvatarType} is on this base.");
        }
    }
}
