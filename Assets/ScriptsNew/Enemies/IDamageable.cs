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
    void TakeDamage(float amount, UnityEngine.Vector3 sourcePosition);
}
