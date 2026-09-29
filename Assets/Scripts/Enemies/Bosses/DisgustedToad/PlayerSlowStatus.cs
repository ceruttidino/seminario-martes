using UnityEngine;

public class PlayerSlowStatus : MonoBehaviour
{
    private float slowPercent;
    private float slowUntil;

    public bool IsSlowed => Time.time < slowUntil;
    public float SpeedMultiplier => IsSlowed ? 1f - slowPercent : 1f;

    public void ApplySlow(float percent, float duration)
    {
        percent = Mathf.Clamp01(percent);
        if (!IsSlowed || percent >= slowPercent) slowPercent = percent;
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
    }

    public void ClearSlow()
    {
        slowUntil = 0f;
        slowPercent = 0f;
    }
}
