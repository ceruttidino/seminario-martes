using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    [SerializeField] Slider vSlider;

    private void Awake()
    {
        if (vSlider == null) return;

        float volume = AudioManager.Instance != null
            ? AudioManager.Instance.MasterVolume
            : AudioListener.volume;

        vSlider.SetValueWithoutNotify(volume);
    }

    public void changeVolume()
    {
        if (vSlider == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.MasterVolume = vSlider.value;
        else
            AudioListener.volume = vSlider.value;
    }
}
