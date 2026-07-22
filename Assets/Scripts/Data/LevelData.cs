using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG.Data
{
    /// <summary>
    /// 关卡数据：敌人类型 + 坐标（以及可选目标点）。
    /// 由 Level Editor 写入，运行时 LevelLoader 读取生成。
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "ARPG/Level Data", order = 2)]
    public class LevelData : ScriptableObject
    {
        public string levelName = "Level 01";

        [Tooltip("玩家出生点（可选，LevelLoader 可传送已有玩家）")]
        public Vector3 playerSpawn = new Vector3(0f, 1f, 0f);

        [Tooltip("是否使用目标点标记")]
        public bool hasGoalPoint;

        public Vector3 goalPoint = new Vector3(0f, 0.1f, 12f);

        public List<EnemySpawnEntry> enemies = new();
    }

    /// <summary>关卡中的单个敌人刷出点。</summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        public EnemyData enemyData;
        public Vector3 position;
        public Vector3 eulerAngles;
    }
}
