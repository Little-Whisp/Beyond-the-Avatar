using UnityEngine;

public class PlayerStartupOverride : MonoBehaviour
{
    public enum PlayerSlot
    {
        Player1,
        Player2,
        Player3,
        Player4
    }

    public PlayerSlot startAs = PlayerSlot.Player1;

    private void Awake()
    {
        switch(startAs)
        {
            case PlayerSlot.Player1:
                PlayerSelection.selectedPlayerName = "P1";
                PlayerSelection.selectedAvatarType = "Avatar1";
                break;

            case PlayerSlot.Player2:
                PlayerSelection.selectedPlayerName = "P2";
                PlayerSelection.selectedAvatarType = "Avatar2";
                break;

            case PlayerSlot.Player3:
                PlayerSelection.selectedPlayerName = "P3";
                PlayerSelection.selectedAvatarType = "Avatar3";
                break;

            case PlayerSlot.Player4:
                PlayerSelection.selectedPlayerName = "P4";
                PlayerSelection.selectedAvatarType = "Avatar4";
                break;
        }
    }
}
