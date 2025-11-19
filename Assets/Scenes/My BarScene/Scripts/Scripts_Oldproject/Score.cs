using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PromptLogEntry
{
    public string prompt;
    public string avatarTag;
    public string timestamp;  
    public string playerID;  
}


public class Score : MonoBehaviour
{
    [Header("Score Display")]
    public TextMeshProUGUI scoreText;

    public List<PromptLogEntry> dataBench = new List<PromptLogEntry>();

    private int currentScore = 0;

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        Debug.Log("[Score] Points Added: " + points);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

}
