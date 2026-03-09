// ─────────────────────────────────────────────────────────────────────────────
//  IDamageable.cs
//
//  Shared interface implemented by anything that can take damage —
//  both Kitty (PlayerStats) and enemies (EnemyStats).
//
//  By keeping this in its own file, any script anywhere in the project
//  can reference it without needing to know where the implementation lives.
// ─────────────────────────────────────────────────────────────────────────────

public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object.
    /// </summary>
    /// <param name="amount">How much damage to deal.</param>
    /// <param name="sourcePosition">Where the hit came from — used for knockback direction.</param>
    void TakeDamage(float amount, UnityEngine.Vector3 sourcePosition);
}
