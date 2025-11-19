using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float totalTime = 20f;
    public TextMeshProUGUI timerText;
    public ResultsUI resultsUI;

    private Score scoreManager;
    private bool hasEnded = false;
    private float currentTime;
    private bool isRunning = false;

    void Start()
    {
        scoreManager = GetComponent<Score>();
        Debug.Log("found score manager");
        Debug.Log(scoreManager);
        ResetTimer();
    }

    void Update()
    {
        if (!isRunning || hasEnded)
            return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        else
        {
            hasEnded = true;
            timerText.text = "00:00";
            Debug.Log("show the score panel");
            resultsUI.DisplayDataBench();

            List<PromptResult> currentSession = new List<PromptResult>();
            foreach (var entry in scoreManager.dataBench)
            {
                currentSession.Add(new PromptResult
                {
                    prompt = entry.prompt,
                    avatarTag = entry.avatarTag,
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    playerID = GameManager.Instance.GetPlayerID()
                });


            }
            PromptSaveSystem.SaveResults(currentSession);
        }
    }

    public void StartTimer()
    {
        ResetTimer();
        isRunning = true;
    }

    public void PauseTimer(bool pause)
    {
        isRunning = !pause;
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    private void ResetTimer()
    {
        currentTime = totalTime;
        hasEnded = false;
        timerText.text = $"{Mathf.FloorToInt(currentTime / 60):00}:{Mathf.FloorToInt(currentTime % 60):00}";
    }
}
