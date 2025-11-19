using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PromptResult
{
    public string prompt;
    public string avatarTag;
    public string timestamp;
    public string playerID;
}

[Serializable]
public class AllPlayerResults
{
    public List<PromptResult> allResults = new List<PromptResult>();
}