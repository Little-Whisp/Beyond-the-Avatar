using System.Collections.Generic;
using UnityEngine;


public class AvatarReactionManager : MonoBehaviour
{
    [System.Serializable]
    public class AvatarReaction
    {
        [TagSelector]
        public string avatarTag;

        [TextArea]
        public List<string> niceLines;
        [TextArea]
        public List<string> meanLines;
        [TextArea]
        public List<string> weirdLines;
    }

    public List<AvatarReaction> reactions;

    public string GetRandomReaction(string avatarTag)
    {
        AvatarReaction match = reactions.Find(r => r.avatarTag == avatarTag);

        if (match != null)
        {
            List<string> pool = new List<string>();

            pool.AddRange(match.niceLines);
            pool.AddRange(match.meanLines);
            pool.AddRange(match.weirdLines);

            if (pool.Count > 0)
            {
                return pool[Random.Range(0, pool.Count)];
            }
        }

        return "😐 ..."; // Default fallback
    }
}
