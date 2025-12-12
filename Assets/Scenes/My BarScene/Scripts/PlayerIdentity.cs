using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    public string playerName;      // Example: "Player1"
    public string avatarType;     

    void Awake()
{
    // Always apply the selected identity BEFORE network join / spawn
    playerName = PlayerSelection.selectedPlayerName;
    avatarType = PlayerSelection.selectedAvatarType;
}

}
