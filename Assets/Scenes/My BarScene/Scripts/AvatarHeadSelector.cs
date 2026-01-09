using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class AvatarHeadSelector : NetworkBehaviour
    {
        [Header("Assign the 3 head model GameObjects (children under Avatar_Head)")]
        [SerializeField] private GameObject[] headModels; // size 3

        [Tooltip("If true, host/bartender will not show any of these heads.")]
        [SerializeField] private bool hideForHost = true;

        // -1 = none, 0..2 = head index
        private NetworkVariable<int> headIndex = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            headIndex.OnValueChanged += (_, __) => ApplyHead();

            if (IsServer)
            {
                // Host is bartender in your setup
                bool isHost = OwnerClientId == NetworkManager.ServerClientId;

                if (hideForHost && isHost)
                {
                    headIndex.Value = -1; // none
                }
                else
                {
                    // Choose based on client id so it's stable:
                    // client 1 -> 0, client 2 -> 1, client 3 -> 2, etc.
                    int idx = (int)((OwnerClientId - 1) % (ulong)headModels.Length);
                    headIndex.Value = Mathf.Clamp(idx, 0, headModels.Length - 1);
                }
            }

            ApplyHead();
        }

        private void OnDestroy()
        {
            headIndex.OnValueChanged -= (_, __) => ApplyHead(); // safe even if not subscribed
        }

        private void ApplyHead()
        {
            if (headModels == null) return;

            for (int i = 0; i < headModels.Length; i++)
            {
                if (headModels[i] != null)
                    headModels[i].SetActive(i == headIndex.Value);
            }

            // If headIndex is -1, disable all
            if (headIndex.Value < 0)
            {
                for (int i = 0; i < headModels.Length; i++)
                    if (headModels[i] != null) headModels[i].SetActive(false);
            }
        }
    }
}
