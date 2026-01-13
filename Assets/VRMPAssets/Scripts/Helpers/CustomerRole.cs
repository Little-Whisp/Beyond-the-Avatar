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

            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
                return;

            // 🔒 ONLY run on the local owning client
            var netObj = GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner)
                return;

            // Customer = non-host local player
            bool isCustomer = !NetworkManager.Singleton.IsServer;

            Debug.Log($"[CustomerRole] Local player → Customer={isCustomer}");

            SetObjects(isCustomer);
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
