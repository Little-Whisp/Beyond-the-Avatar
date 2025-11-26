using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Enables objects for all customers (PlayerNumber >= 2) on the local client.
    /// Bartender (1) and host (0) will NOT see these objects.
    /// </summary>
    public class CustomerRole : MonoBehaviour
    {
        [Tooltip("Objects that all customers (players 2, 3, 4, ...) should see/use.")]
        public GameObject[] customerOnlyObjects;

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

            // All players AFTER 1 (2, 3, 4, ...) are customers
            bool isCustomer = number >= 2;

            foreach (var obj in customerOnlyObjects)
            {
                if (obj != null)
                    obj.SetActive(isCustomer);
            }

            _applied = true;
            enabled = false;
        }
    }
}
