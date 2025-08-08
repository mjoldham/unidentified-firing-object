using System;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

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

    [Serializable]
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
        public int BPM = 180, BeatsBeforeEnd = 8;

        public Sprite Background;
        public float ScrollSpeed;

        public Transform StagePatternPrefab;

        public SpawnInfo[] Spawns;
        public List<string> Prefabs = new List<string>();

        [ContextMenu("Initialise")]
        private void Init()
        {
            Prefabs.Clear();
            if (StagePatternPrefab == null)
            {
                return;
            }

            BaseSpawnable[] spawns = StagePatternPrefab.GetComponentsInChildren<BaseSpawnable>();
            if (spawns.Length == 0)
            {
                return;
            }

            Array.Sort(spawns, (s1, s2) => s1.transform.position.y.CompareTo(s2.transform.position.y));

            Spawns = new SpawnInfo[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                Spawns[i] = new SpawnInfo(spawns[i]);
                if (!Prefabs.Contains(Spawns[i].PrefabName))
                {
                    Prefabs.Add(Spawns[i].PrefabName);
                }
                Debug.Log(Spawns[i].PrefabName);
            }
        }
    }
}
