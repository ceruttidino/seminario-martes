using System;
using UnityEngine;

/// <summary>
/// Opcional: lo implementa quien quiera saber QUIEN le pegó (hoy solo PlayerHealth).
/// IDamageable queda intacto, así no hay que tocar a todos los que ya lo implementan.
/// </summary>
public interface ISourcedDamageable
{
    void TakeDamage(float damage, GameObject source);
}

/// <summary>
/// Foto del atacante tomada en el momento del golpe. Se guarda el sprite y no la referencia al enemigo
/// porque el enemigo puede estar destruido (erizo explotado), invisible (búho, araña en el techo) o en mitad
/// de otra animación cuando se abre la pantalla de Game Over.
/// </summary>
public sealed class DamageSourceInfo
{
    public readonly bool HasEnemyType;
    public readonly EnemyType EnemyType;
    public readonly Sprite Sprite;
    /// <summary>true si el sprite viene de un DamagePortrait con sprite fijo: le gana a los overrides del GameOverManager.</summary>
    public readonly bool IsExplicitPortrait;
    public readonly string Name;

    private DamageSourceInfo(bool hasType, EnemyType type, Sprite sprite, bool explicitPortrait, string name)
    {
        HasEnemyType = hasType;
        EnemyType = type;
        Sprite = sprite;
        IsExplicitPortrait = explicitPortrait;
        Name = name;
    }

    /// <summary>
    /// Resuelve quién es el responsable de un golpe:
    /// 1) Si el source (o un padre) tiene DamageOwner -> el dueño registrado al spawnear (proyectil, charco, tile).
    /// 2) Si no, el propio source: DamagePortrait > EnemyBehaviour > el objeto tal cual (ej: una trampa).
    /// </summary>
    public static DamageSourceInfo From(GameObject source)
    {
        if (source == null) return null;

        DamageOwner owner = source.GetComponentInParent<DamageOwner>();
        if (owner != null && owner.Info != null)
            return owner.Info;

        return Snapshot(source);
    }

    private static DamageSourceInfo Snapshot(GameObject source)
    {
        DamagePortrait portrait = source.GetComponentInParent<DamagePortrait>();
        EnemyBehaviour behaviour = source.GetComponentInParent<EnemyBehaviour>();

        GameObject root = portrait != null ? portrait.gameObject
                        : behaviour != null ? behaviour.gameObject
                        : source;

        Sprite sprite = null;
        bool explicitPortrait = false;

        if (portrait != null)
        {
            sprite = portrait.CurrentSprite(out explicitPortrait);
        }

        if (sprite == null)
        {
            // Primero el SpriteRenderer del propio root; si no tiene, el primero de los hijos.
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sprite = sr.sprite;
        }

        return new DamageSourceInfo(
            behaviour != null,
            behaviour != null ? behaviour.Type : default,
            sprite,
            explicitPortrait,
            root.name);
    }
}

/// <summary>
/// Para llamadas que NO podés cambiar de firma (BossPlayerUtils.DamagePlayer, PlayerGetHurt, etc.):
///     using (DamageContext.Push(gameObject))
///         BossPlayerUtils.DamagePlayer(player, dmg);
/// PlayerHealth.TakeDamage(float) lee DamageContext.Current si no le pasaron source.
/// Es síncrono y Unity corre el gameplay en un solo hilo, así que no hay riesgo de que se mezclen.
/// </summary>
public static class DamageContext
{
    public static GameObject Current { get; private set; }

    public static Scope Push(GameObject source)
    {
        GameObject previous = Current;
        Current = source;
        return new Scope(previous);
    }

    public readonly struct Scope : IDisposable
    {
        private readonly GameObject previous;
        public Scope(GameObject previous) { this.previous = previous; }
        public void Dispose() { Current = previous; }
    }
}

public static class DamageExtensions
{
    /// <summary>
    /// Usar esto en lugar de damageable.TakeDamage(dmg) en cualquier ataque enemigo.
    /// Si el objetivo registra atacantes, le pasa el source; si no, cae al TakeDamage de siempre.
    /// </summary>
    public static void TakeDamageFrom(this IDamageable damageable, float damage, GameObject source)
    {
        if (damageable == null) return;

        if (damageable is ISourcedDamageable sourced)
            sourced.TakeDamage(damage, source);
        else
            damageable.TakeDamage(damage);
    }
}