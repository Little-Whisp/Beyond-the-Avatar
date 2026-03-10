using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class BartenderPromptSession : NetworkBehaviour
{
    [Header("Prompt Source")]
    public PromptManager promptManager;

    [Tooltip("If you want the bartender to alternate antisocial/prosocial order by index")]
    public int bartenderPlayerIndex = 0;

    [Header("UI")]
    public TextMeshProUGUI promptText; // can be world-space near bar

    [Header("Spots")]
    public TriggerZone[] choiceSpots;

    [Header("VFX")]
    public GameObject poofPrefab; // (optional, not needed for local poof anymore)

    [Header("Logging")]
    public PromptResultsLogger logger;

    // Synced prompt string (host writes, everyone reads)
    private readonly NetworkVariable<FixedString512Bytes> currentPromptNet =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<string> sessionPrompts = new();
    private int roundIndex = 0;
    private bool waitingForServe = false;

    private string sessionId;

    public override void OnNetworkSpawn()
    {
        currentPromptNet.OnValueChanged += OnPromptChanged;

        if (promptText != null)
            promptText.text = currentPromptNet.Value.ToString();
    }

    public override void OnNetworkDespawn()
    {
        currentPromptNet.OnValueChanged -= OnPromptChanged;
    }

    private void OnPromptChanged(FixedString512Bytes oldValue, FixedString512Bytes newValue)
    {
        if (promptText != null)
            promptText.text = newValue.ToString();
    }

    private void Start()
    {
        // Host/server sets up session
        if (!IsServer) return;

        if (logger == null) logger = FindFirstObjectByType<PromptResultsLogger>();
        sessionId = Guid.NewGuid().ToString();

        if (promptManager != null)
            promptManager.ResetAllPromptHistory();

        PrepareSessionPrompts();
        BindSpots();

        // IMPORTANT:
        // We do NOT call StartRound(0) here anymore,
        // because YOU want the prompt to show only when a drink is spawned.
        waitingForServe = false;
        currentPromptNet.Value = ""; // optional: clear UI at start
    }

    private void OnEnable()
    {
        GlassResultReceiver.OnCocktailSpawned += OnDrinkSpawned;
    }

    private void OnDisable()
    {
        GlassResultReceiver.OnCocktailSpawned -= OnDrinkSpawned;
    }

    private void OnDrinkSpawned(GameObject drink)
    {
        if (!IsServer) return;
    }

    private void ShowPromptForRound(int index)
    {
        roundIndex = index;

        if (roundIndex >= sessionPrompts.Count)
        {
            EndSession();
            return;
        }

        waitingForServe = true;
        currentPromptNet.Value = sessionPrompts[roundIndex];
    }

    private void PrepareSessionPrompts()
    {
        sessionPrompts.Clear();

        string[] prompts = promptManager != null
            ? promptManager.GetPromptsForPlayer(bartenderPlayerIndex)
            : Array.Empty<string>();

        if (prompts.Length >= 2)
        {
            sessionPrompts.Add(prompts[0]);
            sessionPrompts.Add(prompts[1]);
        }
        else if (prompts.Length == 1)
        {
            sessionPrompts.Add(prompts[0]);
            sessionPrompts.Add("MISSING PROMPT 2");
        }
        else
        {
            sessionPrompts.Add("MISSING PROMPT 1");
            sessionPrompts.Add("MISSING PROMPT 2");
        }
    }

    private void BindSpots()
    {
        if (choiceSpots == null) return;

        foreach (var zone in choiceSpots)
        {
            if (zone == null) continue;
            zone.SetSession(this);
        }
    }

    private void EndSession()
    {
        waitingForServe = false;

        if (logger != null)
            logger.ExportCsv();

        currentPromptNet.Value = ""; // optional clear

        Debug.Log("[BartenderPromptSession] Session complete. CSV exported.");
    }

    // ✅ This is now the “advance” for your new flow:
    // It advances the round counter, BUT does NOT show the next prompt yet.
    // Next prompt appears when the NEXT cocktail spawns.
    public void LocalAdvanceAfterServe(AvatarType chosenTag)
    {
        if (!IsServer) return;
        if (!waitingForServe) return;

        waitingForServe = false;

        string prompt = currentPromptNet.Value.ToString();

        // ✅ Log result
        if (logger != null)
        {
            logger.Add(new PromptResult
            {
                prompt = prompt,
                avatarTag = chosenTag.ToString(),
                timestamp = DateTime.UtcNow.ToString("o"),
                playerID = $"BartenderClientId={NetworkManager.Singleton.LocalClientId} Session={sessionId}",
                roundIndex = roundIndex
            });
        }

        roundIndex++;

        if (roundIndex >= sessionPrompts.Count)
        {
            EndSession();
            return;
        }

        // ✅ Immediately show next prompt
        ShowPromptForRound(roundIndex);
    }

}
