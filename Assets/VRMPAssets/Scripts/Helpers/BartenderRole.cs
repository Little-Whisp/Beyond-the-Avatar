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

            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
                return;

            // HOST = Bartender
            if (!NetworkManager.Singleton.IsServer)
            {
                SetObjects(false);
                _applied = true;
                return;
            }

            SetObjects(true);
            _applied = true;
        }

        void SetObjects(bool state)
        {
            foreach (var obj in bartenderOnlyObjects)
                if (obj) obj.SetActive(state);
        }
    }
}
