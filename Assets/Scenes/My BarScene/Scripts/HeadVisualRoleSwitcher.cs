using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class HeadVisualRoleSwitcher : NetworkBehaviour
    {
        [Header("Assign in Inspector")]
        [SerializeField] private GameObject oldHeadRoot;        // template head mesh root
        [SerializeField] private GameObject clientAvatarRoot;   // your avatar heads root (parent)

        [Header("Optional")]
        [SerializeField] private bool hideLocalPlayersOwnHead = true;

        public override void OnNetworkSpawn()
        {
            // Host = bartender in your setup (StartHost)
            bool isHostPlayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            // This script runs on every player object instance,
            // so use OwnerClientId to decide "who this player is".
            bool thisPlayerIsHost = (OwnerClientId == NetworkManager.ServerClientId);

            // Host player keeps old head, clients get avatar heads
            if (oldHeadRoot) oldHeadRoot.SetActive(thisPlayerIsHost);
            if (clientAvatarRoot) clientAvatarRoot.SetActive(!thisPlayerIsHost);

            // VR comfort: don’t render your own head locally
            if (hideLocalPlayersOwnHead && IsOwner)
            {
                if (oldHeadRoot) oldHeadRoot.SetActive(false);
                if (clientAvatarRoot) clientAvatarRoot.SetActive(false);
            }
        }
    }
}
