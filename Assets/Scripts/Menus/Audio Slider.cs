using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    public enum VolumeChannel
    {
        Master = 0,
        Music = 1,
        Effects = 2
    }

    [SerializeField] private Slider vSlider;
    [SerializeField] private VolumeChannel channel = VolumeChannel.Master;
    [SerializeField] private bool spawnMissingChannelSliders = true;

    private static bool buildingGroup;

    private void Awake()
    {
        if (vSlider == null)
            vSlider = GetComponent<Slider>();

        BindSliderEvents();
        ApplySliderValue();
    }

    private void Start()
    {
        ApplySliderValue();

        if (spawnMissingChannelSliders)
            TryBuildChannelGroup();
    }

    public void changeVolume()
    {
        ApplySliderToManager();
    }

    private void BindSliderEvents()
    {
        if (vSlider == null) return;

        vSlider.onValueChanged.RemoveListener(OnSliderChanged);
        vSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float _)
    {
        ApplySliderToManager();
    }

    private void ApplySliderValue()
    {
        if (vSlider == null) return;
        vSlider.SetValueWithoutNotify(GetChannelVolume());
    }

    private void ApplySliderToManager()
    {
        if (vSlider == null) return;

        float value = vSlider.value;
        if (AudioManager.Instance == null)
        {
            if (channel == VolumeChannel.Master)
                AudioListener.volume = value;
            return;
        }

        switch (channel)
        {
            case VolumeChannel.Music:
                AudioManager.Instance.MusicVolume = value;
                break;
            case VolumeChannel.Effects:
                AudioManager.Instance.SfxVolume = value;
                break;
            default:
                AudioManager.Instance.MasterVolume = value;
                break;
        }

        PlayerPrefs.Save();
    }

    private float GetChannelVolume()
    {
        if (AudioManager.Instance == null)
            return channel == VolumeChannel.Master ? AudioListener.volume : 1f;

        switch (channel)
        {
            case VolumeChannel.Music:
                return AudioManager.Instance.MusicVolume;
            case VolumeChannel.Effects:
                return AudioManager.Instance.SfxVolume;
            default:
                return AudioManager.Instance.MasterVolume;
        }
    }

    private void TryBuildChannelGroup()
    {
        if (buildingGroup) return;

        Transform root = transform.parent;
        if (root == null) return;
        if (root.Find("VolumeSettingsPanel") != null) return;

        buildingGroup = true;
        spawnMissingChannelSliders = false;

        TextMeshProUGUI volumeLabel = FindVolumeLabel(root);
        bool mainMenuStyle = volumeLabel != null;
        TextMeshProUGUI templateLabel = volumeLabel != null
            ? volumeLabel
            : root.GetComponentInChildren<TextMeshProUGUI>(true);

        if (mainMenuStyle)
            HideLegacyWidgets(root, volumeLabel);

        RectTransform panel = CreatePanel(root, mainMenuStyle);
        CreateRow(panel, VolumeChannel.Master, "Master Volume", templateLabel, mainMenuStyle);
        CreateRow(panel, VolumeChannel.Music, "Music Volume", templateLabel, mainMenuStyle);
        CreateRow(panel, VolumeChannel.Effects, "Effects Volume", templateLabel, mainMenuStyle);

        gameObject.SetActive(false);
        buildingGroup = false;
    }

    private static TextMeshProUGUI FindVolumeLabel(Transform root)
    {
        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].text.IndexOf("Volume", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return labels[i];
        }

        return null;
    }

    private void HideLegacyWidgets(Transform root, TextMeshProUGUI templateLabel)
    {
        if (templateLabel == null) return;

        Transform banner = templateLabel.transform.parent;
        if (banner != null && banner != root)
            banner.gameObject.SetActive(false);
        else
            templateLabel.gameObject.SetActive(false);
    }

    private RectTransform CreatePanel(Transform root, bool mainMenuStyle)
    {
        GameObject panelGo = new GameObject("VolumeSettingsPanel", typeof(RectTransform));
        panelGo.transform.SetParent(root, false);

        RectTransform panel = panelGo.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);

        if (mainMenuStyle)
        {
            panel.anchoredPosition = new Vector2(0f, 70f);
            panel.sizeDelta = new Vector2(1180f, 360f);
        }
        else
        {
            panel.anchoredPosition = new Vector2(0f, -85f);
            panel.sizeDelta = new Vector2(780f, 220f);
        }

        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = mainMenuStyle ? 22f : 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(20, 20, 10, 10);

        return panel;
    }

    private void CreateRow(
        RectTransform panel,
        VolumeChannel newChannel,
        string labelText,
        TextMeshProUGUI templateLabel,
        bool mainMenuStyle)
    {
        GameObject rowGo = new GameObject(newChannel + " Row", typeof(RectTransform));
        rowGo.transform.SetParent(panel, false);

        RectTransform row = rowGo.GetComponent<RectTransform>();
        LayoutElement rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = mainMenuStyle ? 96f : 64f;
        rowLayout.minHeight = rowLayout.preferredHeight;

        if (mainMenuStyle)
        {
            Image bg = rowGo.AddComponent<Image>();
            bg.color = new Color(0.268f, 0.121f, 0f, 0.92f);
            bg.raycastTarget = false;
        }

        HorizontalLayoutGroup rowGroup = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = mainMenuStyle ? 28f : 20f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;
        rowGroup.padding = new RectOffset(28, 28, 8, 8);

        CreateLabel(row, labelText, templateLabel, mainMenuStyle);
        CreateRowSlider(row, newChannel, mainMenuStyle);
    }

    private void CreateLabel(Transform row, string text, TextMeshProUGUI templateLabel, bool mainMenuStyle)
    {
        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row, false);

        LayoutElement layout = labelGo.AddComponent<LayoutElement>();
        layout.preferredWidth = mainMenuStyle ? 480f : 280f;
        layout.minWidth = layout.preferredWidth;
        layout.flexibleWidth = 0f;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        tmp.fontSize = mainMenuStyle ? 42f : 26f;
        tmp.color = mainMenuStyle
            ? new Color(0.6f, 0.3f, 0f, 1f)
            : Color.white;

        if (templateLabel != null)
        {
            if (templateLabel.font != null)
                tmp.font = templateLabel.font;
            if (templateLabel.fontSharedMaterial != null)
                tmp.fontSharedMaterial = templateLabel.fontSharedMaterial;
            if (mainMenuStyle)
                tmp.color = templateLabel.color;
        }
    }

    private void CreateRowSlider(Transform row, VolumeChannel newChannel, bool mainMenuStyle)
    {
        GameObject sliderGo = Instantiate(gameObject, row, false);
        sliderGo.name = newChannel + " Slider";
        sliderGo.SetActive(true);

        RectTransform sliderRect = sliderGo.transform as RectTransform;
        sliderRect.localScale = Vector3.one;
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(mainMenuStyle ? 420f : 360f, 24f);

        LayoutElement layout = sliderGo.GetComponent<LayoutElement>();
        if (layout == null)
            layout = sliderGo.AddComponent<LayoutElement>();
        layout.preferredWidth = mainMenuStyle ? 420f : 360f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 24f;

        AudioSlider audio = sliderGo.GetComponent<AudioSlider>();
        audio.spawnMissingChannelSliders = false;
        audio.channel = newChannel;
        audio.vSlider = sliderGo.GetComponent<Slider>();
        audio.BindSliderEvents();
        audio.ApplySliderValue();

        Transform leftoverLabel = sliderGo.transform.Find("VolumeLabel");
        if (leftoverLabel != null)
            Destroy(leftoverLabel.gameObject);
    }
}
