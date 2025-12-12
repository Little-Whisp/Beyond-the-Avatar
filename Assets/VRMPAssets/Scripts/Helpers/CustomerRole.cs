using UnityEngine;
using Unity.Netcode;

namespace XRMultiplayer
{
    public class CustomerRole : MonoBehaviour
    {
        public GameObject[] customerOnlyObjects;
        bool _applied;

        void Update()
        {
            if (_applied)
                return;

            if (!NetworkManager.Singleton.IsListening)
                return;

            // Everyone sees customer UI
            foreach (var obj in customerOnlyObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }

            _applied = true;
        }
    }
}
