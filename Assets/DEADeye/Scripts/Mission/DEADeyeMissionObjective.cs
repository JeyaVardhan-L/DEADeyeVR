using System;
using UnityEngine;

namespace DEADeye
{
    /// <summary>
    /// Configuration asset defining parameters for a sniper mission.
    /// </summary>
    [CreateAssetMenu(fileName = "MissionConfig", menuName = "DEADeye/Mission Objective")]
    public class DEADeyeMissionObjective : ScriptableObject
    {
        [Header("Mission Rules")]
        public string missionTitle = "OPERATION: ROOFTOP OVERWATCH";
        public float missionDurationSeconds = 120f;
        public int initialHostileTargetCount = 8;
        public int maxAllowedEscapes = 3;

        [Header("Difficulty Multipliers")]
        public float recruitSpeedMultiplier = 0.8f;
        public float operativeSpeedMultiplier = 1.0f;
        public float eliteSpeedMultiplier = 1.35f;

        public float GetSpeedMultiplier(MissionDifficulty difficulty)
        {
            switch (difficulty)
            {
                case MissionDifficulty.Recruit: return recruitSpeedMultiplier;
                case MissionDifficulty.Elite: return eliteSpeedMultiplier;
                case MissionDifficulty.Operative:
                default: return operativeSpeedMultiplier;
            }
        }
    }
}

