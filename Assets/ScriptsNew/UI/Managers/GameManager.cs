// ─────────────────────────────────────────────────────────────────────────────
//  GameManager.cs
//
//  General game manager. Death is handled by DeathScreen.cs.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject in MainScene, name it "GameManager"
//  2. Attach this script to it
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
}