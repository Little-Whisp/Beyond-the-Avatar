using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    public string playerName;      // Example: "Player1"
    public string avatarType;      // Example: "GreenAvatar", "Robot", etc.

    void Awake()
{
    // Always apply the selected identity BEFORE network join / spawn
    playerName = PlayerSelection.selectedPlayerName;
    avatarType = PlayerSelection.selectedAvatarType;
}

}
