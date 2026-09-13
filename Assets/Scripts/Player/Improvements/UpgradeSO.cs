using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrades/Upgrade Base", order = 0)]
public abstract class UpgradeSO : ScriptableObject
{
    [Header("Información de la mejora")]
    public string upgradeName = "Nueva Mejora";
    [TextArea(3, 6)] public string description = "Descripción de la mejora";


    [Header("Color de Aura")]
    [SerializeField] public Color aColor;

    public Sprite icon;

    public abstract void Apply(PlayerUpgradeManager manager);
}