using System;
using UnityEngine;

namespace UFO
{
    [Serializable]
    public struct EnemyParams
    {
        public RouteStep CurrentStep;
        public bool CheckForEnemies, ExemptFromCheck;
    }

    public struct SpawnInfo
    {
        public int Beat;
        public string EnemyPrefabName;
        public EnemyParams Parameters;
        public float Lane;

        public SpawnInfo(int beat, string name, EnemyParams enemyParams, float lane)
        {
            Beat = beat;
            EnemyPrefabName = name;
            Parameters = enemyParams;
            Lane = lane;
        }
    }

    [CreateAssetMenu(fileName = "StageSettings", menuName = "Scriptable Objects/StageSettings")]
    public class StageSettings : ScriptableObject
    {
        public AudioClip MusicTrack;
        public int BPM = 180;

        public Sprite Background; // TODO: either pack bgs into one texture ourselves or use sprite atlas to avoid hitching.
        public float ScrollSpeed;

        public Transform StagePatternPrefab;

        [HideInInspector]
        public SpawnInfo[] Spawns;
    }
}
