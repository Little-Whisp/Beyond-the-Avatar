using UnityEngine;
using Unity.Netcode;

namespace XRMultiplayer
{
    public class BartenderRole : MonoBehaviour
    {
        [Header("Bartender-only scene objects")]
        public GameObject[] bartenderOnlyObjects;

        bool _applied;

        void Update()
        {
            if (_applied)
                return;

            // Wait until Netcode is running
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
                return;

            // HOST = Bartender
            if (!NetworkManager.Singleton.IsServer)
            {
                // Client must NEVER see bartender objects
                SetObjects(false);
                _applied = true;
                return;
            }

            Debug.Log("[BartenderRole] Host detected → enabling bartender objects");
            SetObjects(true);
            _applied = true;
        }

        void SetObjects(bool state)
        {
            foreach (var obj in bartenderOnlyObjects)
            {
                if (obj)
                    obj.SetActive(state);
            }
        }
    }
}
