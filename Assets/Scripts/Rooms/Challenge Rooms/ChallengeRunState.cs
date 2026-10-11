using System.Collections.Generic;

/// <summary>
/// Challenges completados en el NIVEL ACTUAL. DungeonManager lo resetea al construir cada piso, asi un
/// challenge se juega como mucho una vez por nivel (y vuelve a poder aparecer en el siguiente).
/// </summary>
public static class ChallengeRunState
{
    public const string Supercontainer = "Supercontainer";
    public const string StealTheItem = "StealTheItem";

    private static readonly HashSet<string> completed = new HashSet<string>();

    public static bool WasCompleted(string challengeId)
    {
        return completed.Contains(challengeId);
    }

    public static void MarkCompleted(string challengeId)
    {
        completed.Add(challengeId);
    }

    public static void Reset()
    {
        completed.Clear();
    }
}
