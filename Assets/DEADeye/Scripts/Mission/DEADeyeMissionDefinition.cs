using System;
using UnityEngine;

namespace DEADeye
{
    public enum MissionId
    {
        BrokenSignal,
        MovingTarget,
        NoClearShot,
        DeadGround
    }

    /// <summary>
    /// Configuration asset defining a complete tactical sniper operation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMissionDefinition", menuName = "DEADeye/Mission Definition")]
    public class DEADeyeMissionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public MissionId missionId = MissionId.BrokenSignal;
        public string operationCode = "OP-01";
        public string operationTitle = "BROKEN SIGNAL";
        public string districtName = "COMMERCIAL DISTRICT & COMMUNICATIONS PLAZA";

        [Header("Tactical Briefing")]
        [TextArea(3, 8)]
        public string tacticalBriefing = 
            "Syndicate hostiles have sabotaged the municipal relay tower and pinned down our two-man comms engineering team in the commercial plaza.\n\n" +
            "Provide precision sniper overwatch from the rooftop perch. Protect both technicians while they restore the transmission array, and eliminate all advancing hostile elements before they compromise the site.";

        [Header("Objectives")]
        public string primaryObjective = "Protect 2 Comms Technicians & Neutralize all 6 Hostiles";
        public string secondaryObjective = "Achieve at least 2 Headshots without friendly casualties";

        [Header("Mission Parameters")]
        public float timeLimitSeconds = 120f;
        public int hostileCount = 6;
        public int maxEscapesAllowed = 2;
        public bool hasTechnicians = true;
        public bool hasCivilians = false;
        public float hostileSpawnInterval = 6.0f;

        [Header("Threat Intel")]
        public string threatLevel = "MODERATE (HIGH CALIBER SMALL ARMS)";
        public string weatherCondition = "OVERCAST / HIGH VISIBILITY";
        public float recommendedZoom = 8.0f;
    }
}
