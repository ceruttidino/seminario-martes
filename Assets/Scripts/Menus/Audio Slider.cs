using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    [SerializeField] Slider vSlider;

    private void Awake()
    {
        vSlider.value = AudioListener.volume;
    }

    public void changeVolume()
    {
        AudioListener.volume = vSlider.value;
    }
}
