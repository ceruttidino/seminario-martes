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

    [Header("Duplicados")]
    [Tooltip("Si esta mejora es una version vieja/duplicada de otra (ej: el pickup viejo de Speed = Gold Ring), asignale aca la que vale. Se tratan como EL MISMO item: no se puede tener dos veces.")]
    public UpgradeSO replacedBy;

    /// <summary>La mejora "real" (sigue la cadena replacedBy). Es la que cuenta para no repetir items.</summary>
    public UpgradeSO Canonical
    {
        get
        {
            UpgradeSO current = this;
            int guard = 0;
            while (current.replacedBy != null && current.replacedBy != current && guard++ < 8)
                current = current.replacedBy;
            return current;
        }
    }

    public abstract void Apply(PlayerUpgradeManager manager);
}
