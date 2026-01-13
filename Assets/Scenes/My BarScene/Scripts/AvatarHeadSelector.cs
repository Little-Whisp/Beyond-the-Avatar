using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class AvatarHeadSelector : NetworkBehaviour
    {
        [Header("Your 3 NEW head model GameObjects (e.g. Abstract/Cartoon/Realistic)")]
        [SerializeField] private GameObject[] headModels; // size 3

        [Header("OLD/DEFAULT head objects to disable (Head meshes, old head, etc.)")]
        [SerializeField] private GameObject[] defaultHeadObjectsToDisable;

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
            headIndex.OnValueChanged += OnHeadIndexChanged;

            if (IsServer)
            {
                bool isHost = OwnerClientId == NetworkManager.ServerClientId;

                if (hideForHost && isHost)
                {
                    headIndex.Value = -1;
                }
                else
                {
                    int idx = (int)((OwnerClientId - 1) % (ulong)headModels.Length);
                    headIndex.Value = Mathf.Clamp(idx, 0, headModels.Length - 1);
                }
            }

            ApplyHead();
        }

        public override void OnNetworkDespawn()
        {
            headIndex.OnValueChanged -= OnHeadIndexChanged;
        }

        private void OnHeadIndexChanged(int oldValue, int newValue) => ApplyHead();

        private void ApplyHead()
        {
            // 1) Toggle NEW heads
            for (int i = 0; i < headModels.Length; i++)
            {
                if (headModels[i] != null)
                    headModels[i].SetActive(i == headIndex.Value);
            }

            // 2) Hide OLD/default head stuff when we are showing a new head
            bool usingNewHead = headIndex.Value >= 0;

            if (defaultHeadObjectsToDisable != null)
            {
                foreach (var go in defaultHeadObjectsToDisable)
                {
                    if (go != null)
                        go.SetActive(!usingNewHead); // disable old when new is active
                }
            }
        }
    }
}
