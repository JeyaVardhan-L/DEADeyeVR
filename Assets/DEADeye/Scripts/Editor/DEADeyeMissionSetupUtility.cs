#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DEADeye.Editor
{
    public static class DEADeyeMissionSetupUtility
    {
        private const string MissionsFolder = "Assets/DEADeye/Data/Missions";

        [MenuItem("DEADeye/Missions/Create Campaign Mission Assets")]
        public static void CreateCampaignMissionAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DEADeye/Data"))
            {
                AssetDatabase.CreateFolder("Assets/DEADeye", "Data");
            }
            if (!AssetDatabase.IsValidFolder(MissionsFolder))
            {
                AssetDatabase.CreateFolder("Assets/DEADeye/Data", "Missions");
            }

            // Mission 01: BROKEN SIGNAL
            CreateMissionAsset(
                "Mission_01_BrokenSignal.asset",
                MissionId.BrokenSignal,
                "OP-01",
                "BROKEN SIGNAL",
                "COMMERCIAL DISTRICT & COMMUNICATIONS PLAZA",
                "Syndicate hostiles have sabotaged the municipal relay tower and pinned down our two-man comms engineering team in the commercial plaza.\n\n" +
                "Provide precision sniper overwatch from the rooftop perch. Protect both technicians while they restore the transmission array, and eliminate all advancing hostile elements before they compromise the site.",
                "Protect 2 Comms Technicians & Neutralize all 6 Hostiles",
                "Achieve at least 2 Headshots without friendly casualties",
                120f,
                6,
                2,
                hasTechs: true,
                hasCivs: false,
                spawnInterval: 6.5f,
                threat: "HIGH (HEAVILY ARMED SYNDICATE STRIKE SQUAD)",
                weather: "OVERCAST / CLEAR SIGHTLINES"
            );

            // Mission 02: MOVING TARGET
            CreateMissionAsset(
                "Mission_02_MovingTarget.asset",
                MissionId.MovingTarget,
                "OP-02",
                "MOVING TARGET",
                "CENTRAL TRANSPORT CORRIDOR",
                "A syndicate motor convoy transporting prototype military decryption hardware is attempting a high-speed transit through the central boulevard toward the north highway tunnel.\n\n" +
                "Interdict the convoy. Neutralize the lead vehicle drivers and armed escort foot patrols before any target vehicle reaches the extraction tunnel.",
                "Intercept Transport Convoy & Neutralize 7 Syndicate Couriers",
                "Eliminate Lead Driver before crossing the central intersection",
                90f,
                7,
                1,
                hasTechs: false,
                hasCivs: false,
                spawnInterval: 4.5f,
                threat: "CRITICAL (RAPID ESCAPE VEHICLES & ARMORED ESCORT)",
                weather: "MIDDAY HEAT / HEAT SHIMMER"
            );

            // Mission 03: NO CLEAR SHOT
            CreateMissionAsset(
                "Mission_03_NoClearShot.asset",
                MissionId.NoClearShot,
                "OP-03",
                "NO CLEAR SHOT",
                "RESIDENTIAL DISTRICT & BROWNSTONE COURTYARD",
                "Syndicate enforcers have taken defensive positions in the residential district, using civilian pedestrians and apartment alleyways as human shielding.\n\n" +
                "ROE STRICTLY ENFORCED: Zero civilian collateral permitted. Positively identify armed hostiles before breaking trigger pressure. One civilian casualty results in mission compromise.",
                "Eliminate 8 Fortified Hostiles without Civilian Casualties",
                "Neutralize all hostiles in under 75 seconds",
                100f,
                8,
                2,
                hasTechs: false,
                hasCivs: true,
                spawnInterval: 6.0f,
                threat: "HIGH (DENSE CIVILIAN POPULATION / HOSTAGES)",
                weather: "DUSK / LOW ANGLE SHADOWS"
            );

            // Mission 04: DEAD GROUND
            CreateMissionAsset(
                "Mission_04_DeadGround.asset",
                MissionId.DeadGround,
                "OP-04",
                "DEAD GROUND",
                "UNFINISHED HIGH-RISE CONSTRUCTION SITE",
                "Syndicate leadership has established a high-ground tactical command post inside an unfinished high-rise skyscraper skeleton. An enemy sniper is positioned on the high crane platform providing cross-fire over the entire sector.\n\n" +
                "Eliminate the crane sniper immediately to suppress incoming fire, then systematically eliminate the ground security squad and Syndicate Commander.",
                "Eliminate Crane Sniper & Neutralize Syndicate Commander + Squad",
                "Eliminate Crane Sniper within the first 30 seconds",
                120f,
                8,
                2,
                hasTechs: false,
                hasCivs: false,
                spawnInterval: 5.5f,
                threat: "MAXIMUM (COUNTER-SNIPER POSITION & HEAVY ASSAULT)",
                weather: "FOG DRIFT / ELEVATED WIND GUSTS"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully created all 4 campaign mission assets!");
        }

        private static void CreateMissionAsset(
            string filename,
            MissionId id,
            string code,
            string title,
            string district,
            string briefing,
            string primary,
            string secondary,
            float timeLimit,
            int hostileCount,
            int maxEscapes,
            bool hasTechs,
            bool hasCivs,
            float spawnInterval,
            string threat,
            string weather)
        {
            string path = $"{MissionsFolder}/{filename}";
            var asset = AssetDatabase.LoadAssetAtPath<DEADeyeMissionDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DEADeyeMissionDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.missionId = id;
            asset.operationCode = code;
            asset.operationTitle = title;
            asset.districtName = district;
            asset.tacticalBriefing = briefing;
            asset.primaryObjective = primary;
            asset.secondaryObjective = secondary;
            asset.timeLimitSeconds = timeLimit;
            asset.hostileCount = hostileCount;
            asset.maxEscapesAllowed = maxEscapes;
            asset.hasTechnicians = hasTechs;
            asset.hasCivilians = hasCivs;
            asset.hostileSpawnInterval = spawnInterval;
            asset.threatLevel = threat;
            asset.weatherCondition = weather;
            asset.recommendedZoom = 8.0f;

            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
