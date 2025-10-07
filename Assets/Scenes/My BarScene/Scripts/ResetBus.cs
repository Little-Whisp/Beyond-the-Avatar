using UnityEngine;

public static class ResetBus
{
    public static System.Action OnReset;
    public static void Fire() => OnReset?.Invoke();
}

