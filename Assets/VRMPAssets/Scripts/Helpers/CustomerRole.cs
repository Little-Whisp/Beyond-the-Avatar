using UnityEngine;
using Unity.Netcode;

namespace XRMultiplayer
{
    public class CustomerRole : MonoBehaviour
    {
        [Header("Customer-only scene objects")]
        public GameObject[] customerOnlyObjects;

        bool _applied;

        void Update()
        {
            if (_applied)
                return;

            // Wait until Netcode is running
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
                return;

            // CLIENT = Customer
            if (NetworkManager.Singleton.IsServer)
            {
                // Host must NEVER see customer objects
                SetObjects(false);
                _applied = true;
                return;
            }

            Debug.Log("[CustomerRole] Client detected → enabling customer objects");
            SetObjects(true);
            _applied = true;
        }

        void SetObjects(bool state)
        {
            foreach (var obj in customerOnlyObjects)
            {
                if (obj)
                    obj.SetActive(state);
            }
        }
    }
}
