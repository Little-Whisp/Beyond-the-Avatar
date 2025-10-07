using UnityEngine;

public static class RbCompat
{
    public static void SetLinVel(this Rigidbody rb, Vector3 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;       // Unity 6+
#else
        rb.velocity = v;             // Unity 2022/2021, etc.
#endif
    }
}
