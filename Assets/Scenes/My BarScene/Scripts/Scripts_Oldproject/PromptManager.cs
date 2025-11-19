using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PromptManager", menuName = "ScriptableObjects/PromptManager", order = 1)]
public class PromptManager : ScriptableObject
{
    [TextArea] public List<string> agencyPrompts;
    [TextArea] public List<string> experiencePrompts;
    [TextArea] public List<string> prosocialPrompts;
    [TextArea] public List<string> antisocialPrompts;

    private Dictionary<string, List<string>> categories;
    private Dictionary<string, List<int>> usedIndicesPerCategory;

    private string[] cycleOrder = new string[] { "Agency", "Experience", "Prosocial", "Antisocial" };
    private int currentCategoryIndex = 0;

    private void OnEnable()
    {
        categories = new Dictionary<string, List<string>>()
        {
            { "Agency", agencyPrompts },
            { "Experience", experiencePrompts },
            { "Prosocial", prosocialPrompts },
            { "Antisocial", antisocialPrompts }
        };

        usedIndicesPerCategory = new Dictionary<string, List<int>>()
        {
            { "Agency", new List<int>() },
            { "Experience", new List<int>() },
            { "Prosocial", new List<int>() },
            { "Antisocial", new List<int>() }
        };
    }

    public string GetNextPrompt()
    {
        // Step 1: Pick the category to use
        string category = cycleOrder[currentCategoryIndex];

        // Step 2: Loop to the next category for next time
        currentCategoryIndex = (currentCategoryIndex + 1) % cycleOrder.Length;

        // Step 3: Grab prompts + usedIndices
        List<string> prompts = categories[category];
        List<int> usedIndices = usedIndicesPerCategory[category];

        // Step 4: Handle empty prompt list
        if (prompts == null || prompts.Count == 0)
            return $"No prompts in {category}.";

        // Step 5: If we've used all, reset just that category's history
        if (usedIndices.Count >= prompts.Count)
            usedIndices.Clear();

        // Step 6: Pick a new unused random prompt
        int index;
        do
        {
            index = Random.Range(0, prompts.Count);
        } while (usedIndices.Contains(index) && usedIndices.Count < prompts.Count);

        usedIndices.Add(index);
        return prompts[index];
    }


    public void ResetCategoryCycle()
    {
        currentCategoryIndex = 0;
        foreach (var list in usedIndicesPerCategory.Values)
        {
            list.Clear();
        }
    }
}
