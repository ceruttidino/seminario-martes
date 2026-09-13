using System.Collections.Generic;

public static class ChallengeRunState
{
    public const string Supercontainer = "Supercontainer";

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
