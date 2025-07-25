using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    [Serializable]
    public struct SpawnInfo
    {
        // If true, no enemies must be present for this to spawn.
        public bool CheckForEnemies;

        // Whether to mirror the enemy's movements.
        public bool IsMirrored;

        public int Bar;
        [Range(0, GameManager.BeatsPerBar - 1)]
        public int Beat;

        public EnemyController EnemyPrefab;
        public Transform RoutePrefab;
    }

    [CreateAssetMenu(fileName = "StageSettings", menuName = "Scriptable Objects/StageSettings")]
    public class StageSettings : ScriptableObject
    {
        public AudioClip MusicTrack;
        public int BPM = 180;

        public Sprite Background; // TODO: either pack bgs into one texture ourselves or use sprite atlas to avoid hitching.
        public float ScrollSpeed;

        public SpawnInfo[] Spawns;
    }
}
