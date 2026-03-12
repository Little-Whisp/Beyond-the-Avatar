using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PromptResult
{
    public string prompt;
    public string avatarTag;

    public float sodaML;
    public float hotSauceML;
    public float strawberryML;

    public string timestamp;
    public string playerID;
    public int roundIndex;
}

[Serializable]
public class AllPlayerResults
{
    public List<PromptResult> allResults = new List<PromptResult>();
}