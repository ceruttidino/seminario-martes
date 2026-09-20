using UnityEngine;

public class SpriteSequence : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 8f;
    [SerializeField] private bool loop = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int index;
    private float timer;
    private bool playing = true;

    public void Play(Sprite[] newFrames, float fps, bool shouldLoop)
    {
        frames = newFrames;
        frameRate = Mathf.Max(1f, fps);
        loop = shouldLoop;
        index = 0;
        timer = 0f;
        playing = frames != null && frames.Length > 0;
        Apply();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!playing || frames == null || frames.Length == 0)
            return;

        timer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, frameRate);
        if (timer < step)
            return;

        timer -= step;
        index++;
        if (index >= frames.Length)
        {
            if (!loop)
            {
                index = frames.Length - 1;
                playing = false;
            }
            else
            {
                index = 0;
            }
        }

        Apply();
    }

    private void Apply()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        if (index >= 0 && index < frames.Length && frames[index] != null)
            spriteRenderer.sprite = frames[index];
    }
}
