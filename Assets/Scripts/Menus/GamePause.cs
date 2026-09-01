using System;
using UnityEngine;

public static class GamePause
{
    public static bool IsPaused { get; private set; }

    public static bool IsGameplayFrozen => IsPaused || Time.timeScale <= 0f;

    public static event Action GameplayFrozen;

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (paused)
            NotifyGameplayFrozen();
    }

    public static void NotifyGameplayFrozen()
    {
        GameplayFrozen?.Invoke();
    }
}
