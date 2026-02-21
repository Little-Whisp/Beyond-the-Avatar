using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer.MiniGames
{
    /// <summary>
    /// Manages the state of the minigame
    /// </summary>
    public class MiniGameManager : NetworkBehaviour
    {
        private const string TIME_FORMAT = "mm':'ss'.'ff";

        public GameState currentNetworkedGameState => networkedGameState.Value;
        public enum GameState { None, PreGame, InGame, PostGame }

        // NetworkVariables must be constructed at declaration
        readonly NetworkVariable<GameState> networkedGameState =
            new(GameState.PreGame, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Dictionary<XRINetworkPlayer, ScoreboardSlot> currentPlayerDictionary = new();

        [Tooltip("The current minigame being used")]
        public MiniGameBase currentMiniGame;

        public bool LocalPlayerInGame => m_LocalPlayerInGame;
        bool m_LocalPlayerInGame = false;

        [Header("UI")]
        public TMP_Text m_GameStateText;
        [SerializeField] TMP_Text m_BestAllText;
        [SerializeField] TMP_Text m_GameNameText;
        [SerializeField, Tooltip("Prefab used for scoreboard ui slots")] GameObject m_PlayerScoreboardSlotPrefab;
        [SerializeField, Tooltip("Prefab used for scoreboard ui slots")] Transform m_ContentListParent;
        [SerializeField] TextButton m_DynamicButton;

        [Header("Video Player")]
        [SerializeField] GameObject m_VideoPlayerObject;
        [SerializeField] GameObject m_TooltipObject;
        [SerializeField] Mask m_TopMask;
        [SerializeField] Mask m_BottomMask;

        [Header("Game")]
        public int maxAllowedPlayers = 4;
        [SerializeField] int m_ReadyUpTimeInSeconds = 15;
        [SerializeField] int m_StartCoutdownTimeInSeconds = 5;
        [SerializeField] int m_PostGameWaitTimeInSeconds = 3;
        [SerializeField] int m_PostGameCountdownTimeInSeconds = 7;
        [SerializeField] GameObject m_TeleportZonesObject;
        [SerializeField] SubTrigger[] m_StartZoneTrigger;

        [Header("Transform References")]
        [SerializeField] Transform m_ScoreboardTransform;
        [SerializeField] Transform m_ScoreboardInGameTransform;
        [SerializeField] Transform m_JoinTeleportTransform;
        [SerializeField] Transform m_LeaveTeleportTransform;
        [SerializeField] Transform m_FinishTeleportTransform;

        [Header("Transform Offsets")]
        [SerializeField] bool m_UseInGameOffset = true;
        [SerializeField, Tooltip("Determines the offset of the canvas during game")] Vector3 m_InGameOffset;
        [SerializeField, Tooltip("Determines the offset of the canvas during the pre-game")] Vector3 m_PreGameOffset;
        [SerializeField] float m_ScoreboardLerpSpeed = 5.0f;

        readonly List<ScoreboardSlot> m_ScoreboardSlots = new();

        NetworkList<ulong> m_CurrentPlayers = new NetworkList<ulong>();
        NetworkList<ulong> m_QueuedUpPlayers = new NetworkList<ulong>();

        readonly NetworkVariable<float> m_BestAllScore =
            new(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        TeleportationProvider m_LocalPlayerTeleportProvider;

        float m_CurrentTimer = 0.0f;
        Pose m_ScoreboardStartPose;
        IEnumerator m_StartGameRoutine;
        IEnumerator m_PostGameRoutine;

        void Start()
        {
            if (currentMiniGame == null)
            {
                TryGetComponent(out currentMiniGame);
            }

            m_LocalPlayerTeleportProvider = FindFirstObjectByType<TeleportationProvider>();

            m_TeleportZonesObject.SetActive(false);
            m_BestAllText.text = "<b>Current Record</b>: No Record Set";
            m_ScoreboardStartPose = new Pose(m_ScoreboardTransform.position, m_ScoreboardTransform.rotation);
            m_GameNameText.text = currentMiniGame.gameName;

            foreach (var trigger in m_StartZoneTrigger)
            {
                trigger.OnTriggerAction += TriggerReadyState;
            }

            SetupPlayerSlots();
        }

        public virtual void Update()
        {
            if (networkedGameState.Value == GameState.InGame)
            {
                float dt = Time.deltaTime;
                m_CurrentTimer += dt;
                currentMiniGame.UpdateGame(dt);
            }
            if ((networkedGameState.Value == GameState.PreGame || (networkedGameState.Value == GameState.InGame && m_UseInGameOffset)) && LocalPlayerInGame)
            {
                UpdateScoreboardPosition();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var trigger in m_StartZoneTrigger)
            {
                trigger.OnTriggerAction -= TriggerReadyState;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            networkedGameState.OnValueChanged += GameStateValueChanged;
            m_BestAllScore.OnValueChanged += BestAllScoreChanged;

            // subscribe AFTER lists exist (they do now)
            m_CurrentPlayers.OnListChanged += UpdatePlayerList;

            if (IsOwner)
            {
                networkedGameState.Value = GameState.PreGame;
                m_BestAllScore.Value = 0;
            }

            UpdateGameState();

            if (networkedGameState.Value == GameState.InGame)
                ResetContestants(true);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // always unsubscribe safely
            networkedGameState.OnValueChanged -= GameStateValueChanged;
            m_BestAllScore.OnValueChanged -= BestAllScoreChanged;
            if (m_CurrentPlayers != null)
                m_CurrentPlayers.OnListChanged -= UpdatePlayerList;

            m_LocalPlayerInGame = false;
            currentPlayerDictionary.Clear();
            m_ScoreboardTransform.SetPositionAndRotation(m_ScoreboardStartPose.position, m_ScoreboardStartPose.rotation);
        }

        private void UpdatePlayerList(NetworkListEvent<ulong> changeEvent)
        {
            if (networkedGameState.Value != GameState.InGame) return;

            foreach (ScoreboardSlot s in m_ScoreboardSlots)
                s.SetSlotOpen();

            currentPlayerDictionary.Clear();

            foreach (var playerId in m_CurrentPlayers)
                AddPlayerToList(playerId);

            for (int i = currentPlayerDictionary.Count; i < m_ScoreboardSlots.Count; i++)
                m_ScoreboardSlots[i].gameObject.SetActive(false);

            for (int i = 0; i < currentPlayerDictionary.Count; i++)
            {
                m_ScoreboardSlots[i].gameObject.SetActive(true);
                m_ScoreboardSlots[i].UpdateScore(0, currentMiniGame.currentGameType);
            }
        }

        void BestAllScoreChanged(float old, float current)
        {
            if (m_BestAllScore.Value <= 0.0f)
            {
                m_BestAllText.text = $"<b>Current Record</b>: No Record Set";
            }
            else
            {
                if (currentMiniGame.currentGameType == MiniGameBase.GameType.Time)
                {
                    TimeSpan time = TimeSpan.FromSeconds(current);
                    m_BestAllText.text = $"<b>Current Record</b>: {time.ToString(TIME_FORMAT)}";
                }
                else
                {
                    m_BestAllText.text = $"<b>Current Record</b>: {current:N0}";
                }
            }
        }

        void GameStateValueChanged(GameState oldState, GameState currentState) => UpdateGameState();

        void UpdateGameState()
        {
            switch (networkedGameState.Value)
            {
                case GameState.PreGame: SetPreGameState(); break;
                case GameState.InGame: SetInGameState(); break;
                case GameState.PostGame: SetPostGameState(); break;
            }
        }

        void SetPreGameState()
        {
            m_LocalPlayerInGame = false;
            if (m_PostGameRoutine != null) StopCoroutine(m_PostGameRoutine);

            currentMiniGame.SetupGame();
            m_ScoreboardTransform.SetPositionAndRotation(m_ScoreboardStartPose.position, m_ScoreboardStartPose.rotation);

            for (int i = 0; i < m_ScoreboardSlots.Count; i++)
                m_ScoreboardSlots[i].gameObject.SetActive(true);

            ResetContestants(false);

            m_GameStateText.text = "Pre Game";

            m_DynamicButton.UpdateButton(AddLocalPlayer, "Join");
            StartCoroutine(ResetReadyZones());
        }

        void SetInGameState()
        {
            m_CurrentTimer = 0.0f;
            ResetContestants(true);

            foreach (var slot in currentPlayerDictionary.Values)
                slot.UpdateScore(0.0f, currentMiniGame.currentGameType);

            for (int i = currentPlayerDictionary.Count; i < m_ScoreboardSlots.Count; i++)
                m_ScoreboardSlots[i].gameObject.SetActive(false);

            foreach (var trigger in m_StartZoneTrigger)
                trigger.subTriggerCollider.enabled = false;

            m_GameStateText.text = "In Progess";

            if (LocalPlayerInGame)
            {
                m_DynamicButton.button.interactable = true;
                PlayerHudNotification.Instance.ShowText($"Game Started!");
                ToggleShrink(true);
                if (!m_UseInGameOffset)
                    m_ScoreboardTransform.SetPositionAndRotation(m_ScoreboardInGameTransform.position, m_ScoreboardInGameTransform.rotation);
            }
            else
            {
                // Allow late-join while the game is running
                m_DynamicButton.button.interactable = true;
                m_DynamicButton.UpdateButton(AddLocalPlayer, "Join");
            }

            currentMiniGame.StartGame();
        }

        void SetPostGameState()
        {
            if (LocalPlayerInGame)
            {
                ToggleShrink(false);
                TeleportToArea(m_LeaveTeleportTransform);
                m_ScoreboardTransform.SetPositionAndRotation(m_ScoreboardStartPose.position, m_ScoreboardStartPose.rotation);
            }

            m_LocalPlayerInGame = false;
            m_TeleportZonesObject.SetActive(false);
            SortPlayers();
            m_GameStateText.text = "Post Game";
            m_DynamicButton.UpdateButton(ResetGame, $"Wait", true, false);
            if (!currentMiniGame.finished)
                currentMiniGame.FinishGame(false);

            m_PostGameRoutine = PostGameRoutine();
            StartCoroutine(m_PostGameRoutine);

            if (currentPlayerDictionary.Count <= 0 && IsOwner)
                networkedGameState.Value = GameState.PreGame;
        }

        IEnumerator PostGameRoutine()
        {
            yield return new WaitForSeconds(m_PostGameWaitTimeInSeconds);
            m_GameStateText.text = "Next Game in";
            for (int i = m_PostGameCountdownTimeInSeconds; i > 0; i--)
            {
                m_DynamicButton.UpdateButton(ResetGame, $"{i}", true, false);
                yield return new WaitForSeconds(1);
            }
            if (IsOwner) networkedGameState.Value = GameState.PreGame;
        }

        void TriggerReadyState(Collider other, bool entered)
        {
            if (other.TryGetComponent(out CharacterController controller))
            {
                // If player enters the trigger and is not already in the game, auto-join
                if (entered && !LocalPlayerInGame)
                {
                    AddLocalPlayer();
                }
                // Still call ready logic for multiplayer sync
                TogglePlayerReadyRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId, entered);

                // If only one player is present and ready, start the game immediately
                if (entered && LocalPlayerInGame && m_QueuedUpPlayers.Count == 1)
                {
                    // Directly start the game for single player
                    if (IsOwner)
                    {
                        StartGameOwnerRpc();
                    }
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        void JoinRejectedRpc(ulong rejectedClientId)
        {
            // Only the rejected client should re-enable their join button
            if (NetworkManager.Singleton.LocalClientId != rejectedClientId)
                return;

            if (!LocalPlayerInGame && m_DynamicButton != null && m_DynamicButton.button != null)
                m_DynamicButton.button.interactable = true;
        }



        [Rpc(SendTo.Everyone)]
        void TogglePlayerReadyRpc(ulong clientId, bool isReady)
        {
            if (XRINetworkGameManager.Instance.TryGetPlayerByID(clientId, out var player))
            {
                if (currentPlayerDictionary.ContainsKey(player))
                    currentPlayerDictionary[player].ToggleReady(isReady);
            }

            if (networkedGameState.Value != GameState.InGame)
                CheckPlayersReady();
        }

        void CheckPlayersReady()
        {
            int readyCount = 0;
            if (m_QueuedUpPlayers.Count <= 0) return;

            foreach (var clientId in m_QueuedUpPlayers)
            {
                if (XRINetworkGameManager.Instance.TryGetPlayerByID(clientId, out var player))
                {
                    if (currentPlayerDictionary.ContainsKey(player) && currentPlayerDictionary[player].isReady)
                        readyCount++;
                }
            }

            // If only one player is present and ready, start the game immediately
            if (readyCount == 1 && m_QueuedUpPlayers.Count == 1)
            {
                if (LocalPlayerInGame && IsOwner)
                {
                    StartGameOwnerRpc();
                }
                return;
            }

            if (readyCount > 0 && readyCount < m_QueuedUpPlayers.Count)
            {
                if (LocalPlayerInGame) m_DynamicButton.button.interactable = false;
                if (m_StartGameRoutine != null) StopCoroutine(m_StartGameRoutine);
                m_StartGameRoutine = StartGameAfterTime(m_ReadyUpTimeInSeconds);
                StartCoroutine(m_StartGameRoutine);
            }
            else if (readyCount <= 0)
            {
                if (LocalPlayerInGame) m_DynamicButton.button.interactable = true;
                if (m_StartGameRoutine != null) StopCoroutine(m_StartGameRoutine);
                if (LocalPlayerInGame) PlayerHudNotification.Instance.ShowText("Game Start Cancelled");
                m_GameStateText.text = "Pre Game";
            }
            else
            {
                if (LocalPlayerInGame) m_DynamicButton.button.interactable = false;
                if (m_StartGameRoutine != null) StopCoroutine(m_StartGameRoutine);
                m_StartGameRoutine = StartGameAfterTime(m_StartCoutdownTimeInSeconds);
                StartCoroutine(m_StartGameRoutine);
            }
        }

        IEnumerator StartGameAfterTime(int countdownTime)
        {
            for (int i = countdownTime; i > 0; i--)
            {
                m_GameStateText.text = $"Game Starting In {i}";
                if (LocalPlayerInGame) PlayerHudNotification.Instance.ShowText(m_GameStateText.text);
                yield return new WaitForSeconds(1);
            }

            m_GameStateText.text = $"Game Starting Now!";

            if (IsOwner)
            {
                m_DynamicButton.button.interactable = false;
                StartGameOwnerRpc();
            }
        }

        [Rpc(SendTo.Owner)]
        void StartGameOwnerRpc()
        {
            for (int i = 0; i < m_QueuedUpPlayers.Count; i++)
                m_CurrentPlayers.Add(m_QueuedUpPlayers[i]);

            m_QueuedUpPlayers.Clear();
            networkedGameState.Value = GameState.InGame;
        }

        [Rpc(SendTo.Owner)]
        public void StopGameOwnerRpc()
        {
            networkedGameState.Value = GameState.PostGame;
            m_CurrentPlayers.Clear();
            if (currentPlayerDictionary.Count > 0)
            {
                float score = currentPlayerDictionary.First().Value.currentScore;
                if (currentMiniGame.currentGameType == MiniGameBase.GameType.Time)
                {
                    if (score < m_BestAllScore.Value || m_BestAllScore.Value <= 0.0f)
                        m_BestAllScore.Value = score;
                }
                else
                {
                    if (score > m_BestAllScore.Value || m_BestAllScore.Value <= 0.0f)
                        m_BestAllScore.Value = score;
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SubmitScoreRpc(float score, ulong clientId, bool finishGameOnScoreSubmit = false)
        {
            if (XRINetworkGameManager.Instance.TryGetPlayerByID(clientId, out XRINetworkPlayer player))
            {
                if (currentPlayerDictionary.ContainsKey(player))
                {
                    currentPlayerDictionary[player].UpdateScore(score, currentMiniGame.currentGameType);
                    if (finishGameOnScoreSubmit)
                    {
                        currentPlayerDictionary[player].isFinished = true;
                        if (player.IsLocalPlayer) FinishGame();
                    }
                }
            }

            SortPlayers();
            CheckIfAllPlayersAreFinished();
        }

        void CheckIfAllPlayersAreFinished()
        {
            bool gameOver = true;
            foreach (KeyValuePair<XRINetworkPlayer, ScoreboardSlot> kvp in currentPlayerDictionary)
            {
                if (!kvp.Value.isFinished) { gameOver = false; break; }
            }

            if (gameOver && IsOwner) StopGameOwnerRpc();
        }

        public void FinishGame()
        {
            if (LocalPlayerInGame) ToggleShrink(false);
            StartCoroutine(TeleportAfterFinish());
        }

        IEnumerator TeleportAfterFinish()
        {
            yield return new WaitForSeconds(1.5f);
            if (networkedGameState.Value == GameState.InGame)
                TeleportToArea(m_FinishTeleportTransform);
        }

        public void AddLocalPlayer()
        {
            m_DynamicButton.button.interactable = false;
            AddPlayerOwnerRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId);
        }

        [Rpc(SendTo.Owner)]
        void AddPlayerOwnerRpc(ulong clientId)
        {
            // Prevent double-join
            // Prevent double-join
            if (m_CurrentPlayers.Contains(clientId) || m_QueuedUpPlayers.Contains(clientId))
            {
                JoinRejectedRpc(clientId);

                return;
            }

            int totalPlayers = currentPlayerDictionary.Count;
            if (totalPlayers >= maxAllowedPlayers)
            {
                JoinRejectedRpc(clientId);

                return;
            }

            // Tell everyone to add/update UI + local teleport
            AddPlayerRpc(clientId);

            if (networkedGameState.Value == GameState.InGame)
            {
                // Late join: add to active players immediately
                m_CurrentPlayers.Add(clientId);
                // m_CurrentPlayers.OnListChanged will rebuild the list/slots
            }
            else
            {
                // PreGame: add to queue
                m_QueuedUpPlayers.Add(clientId);

                // Start immediately when first player joins
                if (m_QueuedUpPlayers.Count == 1)
                    StartGameOwnerRpc();
            }
        }

        //Template AddPlayerOwnerRPC
        // [Rpc(SendTo.Owner)]
        // void AddPlayerOwnerRpc(ulong clientId)
        // {
        //     AddPlayerRpc(clientId);

        //     if (m_QueuedUpPlayers.Count < maxAllowedPlayers)
        //         m_QueuedUpPlayers.Add(clientId);

        //     // ✅ NEW: start immediately if this is the first player
        //     if (m_QueuedUpPlayers.Count == 1)
        //     {
        //         StartGameOwnerRpc();
        //     }
        // }

        [Rpc(SendTo.Everyone)]
        void AddPlayerRpc(ulong clientId)
        {
            int totalPlayers = currentPlayerDictionary.Count;
            if (totalPlayers < maxAllowedPlayers)
            {
                if (networkedGameState.Value != GameState.PostGame)
                    AddPlayerToList(clientId);

                if (clientId == XRINetworkPlayer.LocalPlayer.OwnerClientId)
                {
                    m_LocalPlayerInGame = true;
                    m_TeleportZonesObject.SetActive(true);
                    // Hide or disable the button after joining
                    m_DynamicButton.button.interactable = false;
                    // Alternatively, if you want to just disable interaction:
                    // m_DynamicButton.button.interactable = false;

                    TeleportRequest teleportRequest = new()
                    {
                        destinationPosition = m_JoinTeleportTransform.position,
                        destinationRotation = m_JoinTeleportTransform.rotation,
                        matchOrientation = MatchOrientation.TargetUpAndForward
                    };

                    m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
                    Transform destination = GetClosestReadyPosition(m_JoinTeleportTransform.position);
                    m_ScoreboardTransform.rotation = destination.rotation;
                    m_ScoreboardTransform.position = destination.position + (m_ScoreboardTransform.forward + m_PreGameOffset);
                    PlayerHudNotification.Instance.ShowText($"Joined {currentMiniGame.gameName}");
                }

                int newTotalPlayers = currentPlayerDictionary.Count;
                if (newTotalPlayers >= maxAllowedPlayers && !LocalPlayerInGame && networkedGameState.Value != GameState.PostGame)
                    m_DynamicButton.button.interactable = false;
            }
        }

        void AddPlayerToList(ulong clientId)
        {
            if (XRINetworkGameManager.Instance.TryGetPlayerByID(clientId, out XRINetworkPlayer player))
            {
                if (!currentPlayerDictionary.ContainsKey(player))
                {
                    ScoreboardSlot slot = m_ScoreboardSlots[currentPlayerDictionary.Count];
                    currentPlayerDictionary.Add(player, slot);
                    slot.SetupPlayerSlot(currentPlayerDictionary.Count, player.playerName);
                    player.onDisconnected += PlayerDisconnected;
                }
            }
        }

        public void RemoveLocalPlayer()
        {
            m_DynamicButton.UpdateButton(AddLocalPlayer, "Join", false, false);
            RemovePlayerOwnerRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId);
        }

        [Rpc(SendTo.Owner)]
        void RemovePlayerOwnerRpc(ulong clientId)
        {
            RemovePlayerRpc(clientId);

            if (m_QueuedUpPlayers.Contains(clientId))
                m_QueuedUpPlayers.Remove(clientId);

            if (m_CurrentPlayers.Contains(clientId))
                m_CurrentPlayers.Remove(clientId);
        }

        [Rpc(SendTo.Everyone)]
        void RemovePlayerRpc(ulong clientId)
        {
            if (XRINetworkGameManager.Instance.TryGetPlayerByID(clientId, out XRINetworkPlayer player))
                CheckDroppedPlayer(player);

            if (clientId == XRINetworkPlayer.LocalPlayer.OwnerClientId)
            {
                m_LocalPlayerInGame = false;
                m_TeleportZonesObject.SetActive(false);

                if (networkedGameState.Value != GameState.InGame)
                    m_DynamicButton.button.interactable = true;

                ToggleShrink(false);
                currentMiniGame.RemoveInteractables();
                PlayerHudNotification.Instance.ShowText($"Left {currentMiniGame.gameName}");
                TeleportToArea(m_LeaveTeleportTransform);
                m_ScoreboardTransform.SetPositionAndRotation(m_ScoreboardStartPose.position, m_ScoreboardStartPose.rotation);
            }
        }

        private void PlayerDisconnected(XRINetworkPlayer droppedPlayer) => CheckDroppedPlayer(droppedPlayer);

        void CheckDroppedPlayer(XRINetworkPlayer droppedPlayer)
        {
            ScoreboardSlot removedSlot = null;
            if (currentPlayerDictionary.ContainsKey(droppedPlayer) && networkedGameState.Value != GameState.PostGame)
            {
                removedSlot = currentPlayerDictionary[droppedPlayer];
                removedSlot.SetSlotOpen();
                currentPlayerDictionary.Remove(droppedPlayer);
                droppedPlayer.onDisconnected -= PlayerDisconnected;
                SortPlayers();
            }

            if (IsOwner && m_QueuedUpPlayers.Contains(droppedPlayer.OwnerClientId))
                m_QueuedUpPlayers.Remove(droppedPlayer.OwnerClientId);

            if (networkedGameState.Value == GameState.InGame)
            {
                if (removedSlot != null) removedSlot.gameObject.SetActive(false);

                if (currentPlayerDictionary.Count <= 0)
                {
                    m_DynamicButton.button.interactable = false;
                    if (IsOwner) StopGameOwnerRpc();
                }
                else
                {
                    CheckIfAllPlayersAreFinished();
                }
            }
            else if (networkedGameState.Value == GameState.PreGame)
            {
                if (currentPlayerDictionary.Count > 0)
                {
                    if (currentPlayerDictionary.Count >= maxAllowedPlayers)
                        m_DynamicButton.button.interactable = false;
                    else
                        m_DynamicButton.button.interactable = true;
                }
                CheckPlayersReady();
            }
        }

        void SortPlayers()
        {
            if (currentMiniGame.currentGameType == MiniGameBase.GameType.Time)
                currentPlayerDictionary = currentPlayerDictionary.OrderBy(x => x.Value.currentScore).ToDictionary(x => x.Key, x => x.Value);
            else
                currentPlayerDictionary = currentPlayerDictionary.OrderByDescending(x => x.Value.currentScore).ToDictionary(x => x.Key, x => x.Value);

            OrganizePlayerList();
        }

        void OrganizePlayerList()
        {
            int currentPlace = 1;
            foreach (var slot in currentPlayerDictionary.Values)
            {
                slot.transform.SetSiblingIndex(currentPlace - 1);
                slot.UpdatePlace(currentPlace++);
            }
            m_ScoreboardSlots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        void ToggleShrink(bool toggle)
        {
            m_BottomMask.enabled = toggle;
            m_BottomMask.graphic.enabled = toggle;

            m_TopMask.enabled = !toggle;
            m_TopMask.graphic.enabled = !toggle;
            m_GameNameText.enabled = !toggle;
            m_VideoPlayerObject.SetActive(!toggle);
            m_TooltipObject.SetActive(!toggle);
        }

        void UpdateScoreboardPosition()
        {
            Vector3 offset = networkedGameState.Value == GameState.InGame ? m_InGameOffset : m_PreGameOffset;
            Transform destination = GetClosestReadyPosition(XRINetworkPlayer.LocalPlayer.transform.position);
            m_ScoreboardTransform.rotation = destination.rotation;
            Vector3 destinationPosition = destination.position + (m_ScoreboardTransform.right * offset.x) + (m_ScoreboardTransform.up * offset.y) + (m_ScoreboardTransform.forward * offset.z);
            m_ScoreboardTransform.position = Vector3.Lerp(m_ScoreboardTransform.position, destinationPosition, Time.deltaTime * m_ScoreboardLerpSpeed);
        }

        Transform GetClosestReadyPosition(Vector3 position)
        {
            Transform closestTransform = null;
            foreach (var readyZone in m_StartZoneTrigger)
            {
                if (closestTransform == null || Vector3.Distance(readyZone.transform.position, position) < Vector3.Distance(closestTransform.position, position))
                    closestTransform = readyZone.transform;
            }
            return closestTransform;
        }

        public void UpdatePlayerScores()
        {
            foreach (var p in currentPlayerDictionary)
            {
                if (!p.Value.isFinished)
                    p.Value.UpdateScore(m_CurrentTimer, currentMiniGame.currentGameType);
            }
        }

        void ResetGame()
        {
            networkedGameState.Value = GameState.PreGame;
            SetPreGameState();
        }

        IEnumerator ResetReadyZones()
        {
            yield return new WaitForSeconds(1.0f);
            foreach (var trigger in m_StartZoneTrigger)
                trigger.subTriggerCollider.enabled = true;
        }

        void ResetContestants(bool showGamePlayers)
        {
            foreach (ScoreboardSlot s in m_ScoreboardSlots)
                s.SetSlotOpen();

            currentPlayerDictionary.Clear();

            if (showGamePlayers)
            {
                foreach (var playerId in m_CurrentPlayers)
                    AddPlayerToList(playerId);
            }
            else
            {
                foreach (var playerId in m_QueuedUpPlayers)
                    AddPlayerToList(playerId);
            }
        }

        void TeleportToArea(Transform teleportTransform)
        {
            TeleportRequest teleportRequest = new TeleportRequest
            {
                destinationPosition = teleportTransform.position,
                destinationRotation = teleportTransform.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward
            };
            m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
        }

        void UpdateBestScore(float score, TMP_Text textAsset)
        {
            if (m_BestAllScore.Value <= 0.0f)
            {
                textAsset.text = $"<b>Current Record</b>: No Record Set";
            }
            else
            {
                if (currentMiniGame.currentGameType == MiniGameBase.GameType.Time)
                {
                    if (score <= m_BestAllScore.Value && m_BestAllScore.Value > 0.0f)
                    {
                        TimeSpan time = TimeSpan.FromSeconds(score);
                        textAsset.text = $"<b>Current Record</b>: {time.ToString(TIME_FORMAT)}";
                    }
                }
                else
                {
                    if (score >= m_BestAllScore.Value && m_BestAllScore.Value > 0.0f)
                        textAsset.text = $"<b>Current Record</b>:  {score:N0}";
                }
            }
        }

        void SetupPlayerSlots()
        {
            for (int i = 0; i < maxAllowedPlayers; i++)
            {
                Instantiate(m_PlayerScoreboardSlotPrefab, m_ContentListParent).TryGetComponent(out ScoreboardSlot slot);
                m_ScoreboardSlots.Add(slot);
                slot.SetSlotOpen();
            }
        }
    }
}
