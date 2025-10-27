using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using Unity.XR.CoreUtils; // Needed for XROrigin

// ---- XRI v2/v3 compatibility for XRBaseInteractor ----
#if UNITY_XR_INTERACTION_TOOLKIT_3_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using InteractorT = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
#else
using InteractorT = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
#endif

public class PlayerSpawnController : NetworkBehaviour
{
    [Header("Assign in Inspector (or leave blank to auto-find by Tag)")]
    [Tooltip("Canvas (root) for the Whack-A-Pig pre-game UI. Tag: MiniGameUI")]
    public GameObject preGameUIRoot;     // tagged "MiniGameUI"

    [Tooltip("Collider wall/box that pens the player at the mini-game. Tag: MiniGameBarrier")]
    public Collider miniGameBarrier;     // tagged "MiniGameBarrier"

    [Tooltip("Locomotion providers that should be disabled for non-hosts.")]
    public List<LocomotionProvider> locomotionProviders = new();

    [Tooltip("Interactors that should ONLY talk to the mini-game layer for non-hosts.")]
    public List<InteractorT> interactors = new();

    [Tooltip("Interaction Layer used by the hammers & pigs (create one named 'MiniGame').")]
    public InteractionLayerMask miniGameLayer; // auto-fills if None

    [Header("Optional (auto found if empty)")]
    [Tooltip("XR Origin / rig root. If empty, found automatically.")]
    public Transform rigRoot; // The object you normally move/teleport (XR Origin)

    InteractionLayerMask[] _originalLayers;
    bool _locked;

    void Awake()
    {
        // Auto-find XR Origin if rigRoot isn't assigned
        if (rigRoot == null)
        {
            XROrigin xrOrigin = null;
#if UNITY_2023_1_OR_NEWER
            xrOrigin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
#else
            try { xrOrigin = Object.FindObjectOfType<XROrigin>(true); }
            catch { xrOrigin = Object.FindObjectOfType<XROrigin>(); }
#endif
            rigRoot = xrOrigin ? xrOrigin.transform : transform;
        }

        TryAutoWireSceneRefs();
    }

    void TryAutoWireSceneRefs()
    {
        // Find UI by tag
        if (!preGameUIRoot)
        {
            var ui = GameObject.FindWithTag("MiniGameUI");
            if (ui) preGameUIRoot = ui;
        }

        // Find barrier by tag
        if (!miniGameBarrier)
        {
            var go = GameObject.FindWithTag("MiniGameBarrier");
            if (go) miniGameBarrier = go.GetComponent<Collider>();
        }

        // Auto get "MiniGame" interaction layer if not set
        if (miniGameLayer.value == 0)
        {
            miniGameLayer = InteractionLayerMask.GetMask("MiniGame");
        }
    }

    public override void OnNetworkSpawn()
    {
        TryAutoWireSceneRefs();

        // Host (IsServer && IsOwner) keeps normal controls.
        // Client player (IsOwner && !IsServer) → lock immediately.
        if (IsOwner && !IsServer)
        {
            LockToMiniGame(true);
        }
    }

    /// <summary>Called by SpawnSystem on the joining client.</summary>
    [ClientRpc]
    public void ApplySpawnClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams sendTo = default)
    {
        if (!IsOwner) return; // only move the local player rig

        TryAutoWireSceneRefs();

        if (rigRoot != null)
            rigRoot.SetPositionAndRotation(pos, rot);

        if (preGameUIRoot != null)
            preGameUIRoot.SetActive(true);

        // Non-hosts are locked at the mini-game
        if (!IsServer)
            LockToMiniGame(true);
    }

    /// <summary>Enable/disable movement and restrict interactions to the mini-game.</summary>
    public void LockToMiniGame(bool locked)
    {
        if (_locked == locked) return;
        _locked = locked;

        // 1) Locomotion: off when locked
        if (locomotionProviders != null)
        {
            foreach (var lp in locomotionProviders)
                if (lp != null) lp.enabled = !locked;
        }

        // 2) Optional physical barrier
        if (miniGameBarrier != null)
            miniGameBarrier.enabled = locked;

        // 3) Interaction layers: store originals once, then force to mini-game layer while locked
        if ((_originalLayers == null || _originalLayers.Length == 0) && interactors != null)
        {
            _originalLayers = new InteractionLayerMask[interactors.Count];
            for (int i = 0; i < interactors.Count; i++)
                _originalLayers[i] = interactors[i]
                    ? interactors[i].interactionLayers
                    : new InteractionLayerMask();
        }

        if (interactors != null)
        {
            for (int i = 0; i < interactors.Count; i++)
            {
                var it = interactors[i];
                if (!it) continue;
                it.interactionLayers = locked
                    ? miniGameLayer
                    : (_originalLayers != null && i < _originalLayers.Length ? _originalLayers[i] : it.interactionLayers);
            }
        }
    }

    // Call this from your mini-game “Leave/Finish” flow if you want to restore movement later.
    public void UnlockFromMiniGame() => LockToMiniGame(false);
}
