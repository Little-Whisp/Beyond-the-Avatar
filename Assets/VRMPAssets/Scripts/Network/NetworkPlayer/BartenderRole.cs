using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Enables objects only for the bartender (PlayerNumber == 1) on the local client.
    /// Attach this to something in your scene and assign bartender-only objects in the Inspector.
    /// </summary>
    public class BartenderRole : MonoBehaviour
    {
        [Tooltip("Objects that only the bartender (Player 1) should see/use.")]
        public GameObject[] bartenderOnlyObjects;

        bool _applied;

        void Update()
        {
            if (_applied)
                return;

            var localPlayer = XRINetworkPlayer.LocalPlayer;
            if (localPlayer == null)
                return; // local player not spawned yet

            int number = localPlayer.PlayerNumber.Value;
            if (number < 0)
                return; // not assigned yet

            bool isBartender = localPlayer.IsBartender;

            foreach (var obj in bartenderOnlyObjects)
            {
                if (obj != null)
                    obj.SetActive(isBartender);
            }

            // We only need to do this once.
            _applied = true;
            enabled = false;
        }
    }
}
