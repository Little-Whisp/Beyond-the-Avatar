using UnityEngine;
using Unity.Netcode;

namespace XRMultiplayer
{
    public class BartenderRole : MonoBehaviour
    {
        public GameObject[] bartenderOnlyObjects;
        bool _applied;

        void Update()
        {
            if (_applied)
                return;

            if (!NetworkManager.Singleton.IsListening)
                return;

            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            ulong serverClientId = NetworkManager.ServerClientId;

            bool isBartender = localClientId == serverClientId;


            Debug.Log(
                $"[BartenderRole] LocalClientId={localClientId} ServerClientId={serverClientId} → Bartender={isBartender}"
            );

            foreach (var obj in bartenderOnlyObjects)
            {
                if (obj != null)
                    obj.SetActive(isBartender);
            }

            _applied = true;
        }
    }
}
