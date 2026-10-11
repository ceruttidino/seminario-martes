using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lleva la cuenta de las mejoras (items) de la run para que cada una exista UNA sola vez:
///  - owned: las que el player ya tiene (no se pueden volver a generar ni recoger).
///  - reserved: las que ya estan "puestas" en el mundo o en la tienda y todavia no se recogieron
///    (evita que dos rooms generen el mismo item antes de que alguien lo agarre).
/// Las mejoras viejas/duplicadas se resuelven a su version "real" con UpgradeSO.Canonical.
/// </summary>
public static class UpgradeRegistry
{
    private static readonly HashSet<UpgradeSO> owned = new HashSet<UpgradeSO>();
    private static readonly Dictionary<UpgradeSO, Object> reserved = new Dictionary<UpgradeSO, Object>();

    public static UpgradeSO Resolve(UpgradeSO upgrade)
    {
        return upgrade != null ? upgrade.Canonical : null;
    }

    public static bool IsOwned(UpgradeSO upgrade)
    {
        UpgradeSO real = Resolve(upgrade);
        return real != null && owned.Contains(real);
    }

    /// <summary>True si se puede generar un item nuevo de esta mejora (no la tenemos y nadie la reservo).</summary>
    public static bool CanSpawn(UpgradeSO upgrade)
    {
        UpgradeSO real = Resolve(upgrade);
        if (real == null) return false;
        if (owned.Contains(real)) return false;

        return !IsReservedByOther(real, null);
    }

    /// <summary>Reserva la mejora para "claimer". Es idempotente: si ya era suya devuelve true.</summary>
    public static bool TryReserve(UpgradeSO upgrade, Object claimer)
    {
        UpgradeSO real = Resolve(upgrade);
        if (real == null || claimer == null) return false;
        if (owned.Contains(real)) return false;

        if (reserved.TryGetValue(real, out Object current) && current != null)
            return current == claimer;

        reserved[real] = claimer;
        return true;
    }

    public static bool IsReservedByOther(UpgradeSO upgrade, Object claimer)
    {
        UpgradeSO real = Resolve(upgrade);
        if (real == null) return false;

        return reserved.TryGetValue(real, out Object current) && current != null && current != claimer;
    }

    public static void Release(UpgradeSO upgrade, Object claimer)
    {
        UpgradeSO real = Resolve(upgrade);
        if (real == null) return;

        if (reserved.TryGetValue(real, out Object current) && (current == claimer || current == null))
            reserved.Remove(real);
    }

    /// <summary>El player la recogio: pasa a "owned" y se libera la reserva.</summary>
    public static void MarkOwned(UpgradeSO upgrade)
    {
        UpgradeSO real = Resolve(upgrade);
        if (real == null) return;

        owned.Add(real);
        reserved.Remove(real);
    }

    /// <summary>Al cambiar de piso: los pickups sin recoger se destruyen, sus items vuelven a estar disponibles.</summary>
    public static void ClearReservations()
    {
        reserved.Clear();
    }

    /// <summary>Nueva run.</summary>
    public static void ResetRun()
    {
        owned.Clear();
        reserved.Clear();
    }
}
