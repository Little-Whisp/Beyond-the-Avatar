using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class PromptManager : NetworkBehaviour
    {
        // ...existing variables...

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // ...existing code...
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // ...existing code...
        }

        // ...existing methods...

        public void GenerateNewPrompt()
        {
            // Call your existing prompt logic here.
            // Replace ShowNextPrompt() with your actual method if different.
            ShowNextPrompt();
        }

        void ShowNextPrompt()
        {
            // ...existing prompt logic...
        }
    }
}