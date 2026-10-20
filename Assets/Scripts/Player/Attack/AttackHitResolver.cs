using System;
using System.Collections.Generic;
using UnityEngine;

// NUEVO (archivo completo)
// Lógica de impacto compartida por QuickAttack y AreaAttack.
// Antes estaba duplicada en los dos scripts. Además arregla un bug:
// si un enemigo tenía varios colliders dentro del área, recibía el daño varias veces.
public static class AttackHitResolver
{
    public struct HitResult
    {
        public int enemiesHit;   // IDamageable dañados
        public int objectsHit;   // tachos, shells, etc.
        public bool AnyHit => enemiesHit + objectsHit > 0;
    }

    // Evita golpear dos veces al mismo objetivo en un mismo ataque
    private static readonly HashSet<Component> alreadyHit = new HashSet<Component>();

    /// <param name="hits">Colliders devueltos por el Overlap</param>
    /// <param name="origin">Posición del player (para calcular el punto de impacto)</param>
    /// <param name="getDirection">Dirección del golpe/knockback para cada collider</param>
    public static HitResult Resolve(Collider2D[] hits, Vector2 origin, float damage, float knockbackForce,
                                    Func<Collider2D, Vector2> getDirection)
    {
        HitResult result = default;
        alreadyHit.Clear();

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            Vector2 direction = getDirection(hit);

            // El shell no se "daña": se empuja en la dirección del golpe.
            TurtleShell shell = hit.GetComponentInParent<TurtleShell>();
            if (shell != null)
            {
                if (!alreadyHit.Add(shell)) continue;
                shell.Push(direction);
                result.objectsHit++;
                SpawnHitFx(hit, origin, direction);
                continue;
            }

            Supercontainer supercontainer = hit.GetComponentInParent<Supercontainer>();
            if (supercontainer != null)
            {
                if (!alreadyHit.Add(supercontainer)) continue;
                supercontainer.NotifyPlayerHit();
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                Component key = damageable as Component;
                if (key != null && !alreadyHit.Add(key)) continue;

                // Reacción ANTES del daño: si el golpe lo mata, la lógica de muerte
                // puede pisar el knockback si quiere (ej: frenar el cuerpo).
                HitReaction reaction = hit.GetComponentInParent<HitReaction>();
                if (reaction != null)
                    reaction.ReceiveHit(direction, knockbackForce);

                damageable.TakeDamage(damage);
                result.enemiesHit++;
                SpawnHitFx(hit, origin, direction);
            }
            else if (hit.TryGetComponent(out BreakableTrash trash))
            {
                if (!alreadyHit.Add(trash)) continue;

                // Los tachos también pueden tener HitReaction (sin Rigidbody: solo flash + squash)
                HitReaction reaction = hit.GetComponentInParent<HitReaction>();
                if (reaction != null)
                    reaction.ReceiveHit(direction, 0f);

                trash.TakeHit(damage);
                result.objectsHit++;
                SpawnHitFx(hit, origin, direction);
            }
        }

        return result;
    }

    private static void SpawnHitFx(Collider2D hit, Vector2 origin, Vector2 direction)
    {
        CombatFeedback fx = CombatFeedback.Instance;
        if (fx == null) return;

        // Punto del collider más cercano al player = donde "conecta" el golpe
        Vector2 hitPoint = hit.ClosestPoint(origin);
        fx.SpawnHitVfx(hitPoint, direction);
    }
}