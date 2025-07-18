using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    [Serializable]
    public struct SpawnInfo // TODO: log bars and beats for ease of use.
    {
        public int Bar;
        [Range(0, GameManager.BeatsPerBar - 1)]
        public int Beat;
        [Range(-(GameManager.NumLanes - 1) / 2, (GameManager.NumLanes - 1) / 2)]
        public int Lane;

        public EnemyBase Enemy;
    }

    [CreateAssetMenu(fileName = "StageSettings", menuName = "Scriptable Objects/StageSettings")]
    public class StageSettings : ScriptableObject
    {
        public AudioClip MusicTrack;
        public int BPM = 180;
        public SpawnInfo[] Spawns;

        [HideInInspector]
        public Dictionary<(int, int), (int, EnemyBase)> Timeline = new Dictionary<(int, int), (int, EnemyBase)>();
    }
}
