using System;
using UnityEngine;

namespace DEADeye
{
    public enum MissionId
    {
        DeadAir,
        TheLastStop,
        SilentEntry,
        DeadGround
    }

    /// <summary>
    /// Configuration asset defining a complete tactical sniper operation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMissionDefinition", menuName = "DEADeye/Mission Definition")]
    public class DEADeyeMissionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public MissionId missionId = MissionId.DeadAir;
        public string operationCode = "OP-01";
        public string operationTitle = "DEAD AIR";
        public string districtName = "CENTRAL COMMERCIAL PLAZA & COMMS INTERCHANGE";

        [Header("Status & Progression")]
        public bool isLocked = false;
        public string statusText = "AVAILABLE";
        public string difficulty = "MODERATE (HIGH CALIBER SMALL ARMS)";

        [Header("Tactical Briefing")]
        [TextArea(3, 8)]
        public string tacticalBriefing = 
            "Syndicate hostiles have deployed a frequency jammer to hijack the city relay tower and plunge the district into a radio blackout.\n\n" +
            "Two civilian communications engineers—Elena and Marcus—have slipped into the plaza below under cover to access the municipal broadcast relay tower console and restore emergency transmission.\n\n" +
            "Provide precision sniper overwatch from the rooftop perch. Cut jammer power, protect both technicians, eliminate Commander Vance, and escort their extraction.";

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
        public string threatLevel = "HIGH (HEAVILY ARMED SYNDICATE STRIKE SQUAD)";
        public string weatherCondition = "OVERCAST / HIGH VISIBILITY";
        public float recommendedZoom = 8.0f;
    }
}
