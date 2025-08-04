using System;
using UnityEngine;

namespace UFO
{
    public enum SpawnCondition
    {
        None,
        NoEnemies,
        CarryOver
    }

    [Serializable]
    public struct SpawnParams
    {
        public RouteStep CurrentStep;
        public SpawnCondition Condition;
        public bool ExemptFromCheck;
    }

    public struct SpawnInfo
    {
        public int Beat;
        public float Lane;
        public string PrefabName;
        public SpawnParams Parameters;
        
        public SpawnInfo(BaseSpawnable spawnable)
        {
            Beat = Mathf.RoundToInt(spawnable.transform.position.y);
            Lane = spawnable.transform.position.x;
            PrefabName = spawnable.gameObject.name.Split(' ')[0];
            Parameters = spawnable.Parameters;
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
