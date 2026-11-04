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

            // Operation 01: DEAD AIR (Available / Playable)
            CreateMissionAsset(
                "Mission_01_DeadAir.asset",
                MissionId.DeadAir,
                "OP-01",
                "DEAD AIR",
                "CENTRAL COMMERCIAL PLAZA & COMMS INTERCHANGE",
                "A hostile syndicate strike team has deployed an emergency frequency jammer to hijack the municipal communication grid, plunging the entire metropolitan district into a radio blackout.\n\n" +
                "You are positioned in high-angle rooftop overwatch above the Central Commercial Plaza. Two unarmed civilian field technicians—Elena and Marcus—have slipped into the plaza under cover to access the broadcast relay tower.\n\n" +
                "Cut power to the syndicate jammer by sniping its high-voltage power junction on the utility pole, protect Elena and Marcus from advancing assault waves, neutralize Commander Vance, and cover their extraction.",
                "Protect 2 Comms Technicians & Neutralize all 6 Hostiles",
                "Achieve at least 2 Headshots without friendly casualties",
                120f,
                6,
                2,
                hasTechs: true,
                hasCivs: false,
                spawnInterval: 6.5f,
                threat: "HIGH (HEAVILY ARMED SYNDICATE STRIKE SQUAD)",
                weather: "OVERCAST / CLEAR SIGHTLINES",
                locked: false,
                status: "AVAILABLE",
                difficulty: "MODERATE (HIGH CALIBER SMALL ARMS)"
            );

            // Operation 02: THE LAST STOP (Locked)
            CreateMissionAsset(
                "Mission_02_TheLastStop.asset",
                MissionId.TheLastStop,
                "OP-02",
                "THE LAST STOP",
                "TRANSPORT INTERCHANGE & BUS TERMINAL DEPOT",
                "A syndicate motor convoy transporting prototype military decryption hardware is staging a high-speed breakout through the central transport terminal toward the highway tunnel.\n\n" +
                "Interdict the convoy before they escape. Neutralize armed escort foot patrols and courier vehicles before any target vehicle reaches the extraction tunnel.",
                "Intercept Transport Convoy & Neutralize 7 Syndicate Couriers",
                "Eliminate Lead Driver before crossing the central intersection",
                90f,
                7,
                1,
                hasTechs: false,
                hasCivs: false,
                spawnInterval: 4.5f,
                threat: "CRITICAL (RAPID ESCAPE VEHICLES & ARMORED ESCORT)",
                weather: "MIDDAY HEAT / HEAT SHIMMER",
                locked: true,
                status: "LOCKED [COMPLETE OP-01]",
                difficulty: "HARD (ARMORED CONVOY & RAPID TRANSIT)"
            );

            // Operation 03: SILENT ENTRY (Locked)
            CreateMissionAsset(
                "Mission_03_SilentEntry.asset",
                MissionId.SilentEntry,
                "OP-03",
                "SILENT ENTRY",
                "RESIDENTIAL DISTRICT & BROWNSTONE COURTYARD",
                "Syndicate enforcers have fortified defensive positions throughout the residential district, using civilian pedestrians and narrow brownstone alleyways as human shielding.\n\n" +
                "ROE STRICTLY ENFORCED: Zero civilian collateral permitted. Positively identify armed hostiles before breaking trigger pressure. One civilian casualty results in immediate mission failure.",
                "Eliminate 8 Fortified Hostiles without Civilian Casualties",
                "Neutralize all hostiles in under 75 seconds",
                100f,
                8,
                2,
                hasTechs: false,
                hasCivs: true,
                spawnInterval: 6.0f,
                threat: "HIGH (DENSE CIVILIAN POPULATION / HOSTAGES)",
                weather: "DUSK / LOW ANGLE SHADOWS",
                locked: true,
                status: "LOCKED [COMPLETE OP-02]",
                difficulty: "EXPERT (CIVILIAN HOSTAGES & STRICT ROE)"
            );

            // Operation 04: DEAD GROUND (Locked)
            CreateMissionAsset(
                "Mission_04_DeadGround.asset",
                MissionId.DeadGround,
                "OP-04",
                "DEAD GROUND",
                "UNFINISHED HIGH-RISE CONSTRUCTION SITE & CRANE",
                "Syndicate leadership has established a high-ground tactical command post inside an unfinished high-rise skyscraper skeleton. An enemy sniper is positioned on the 28m tower crane platform providing cross-fire over the sector.\n\n" +
                "Eliminate the crane marksman immediately to suppress incoming fire, then systematically neutralize the ground security assault squad and the Syndicate Commander.",
                "Eliminate Crane Sniper & Neutralize Syndicate Commander + Squad",
                "Eliminate Crane Sniper within the first 30 seconds",
                120f,
                8,
                2,
                hasTechs: false,
                hasCivs: false,
                spawnInterval: 5.5f,
                threat: "MAXIMUM (COUNTER-SNIPER POSITION & HEAVY ASSAULT)",
                weather: "FOG DRIFT / ELEVATED WIND GUSTS",
                locked: true,
                status: "LOCKED [COMPLETE OP-03]",
                difficulty: "MAXIMUM (HIGH-GROUND COUNTER-SNIPER)"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DEADeyeMissionSetupUtility] Successfully created all 4 campaign mission assets!");
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
            string weather,
            bool locked,
            string status,
            string difficulty)
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
            asset.isLocked = locked;
            asset.statusText = status;
            asset.difficulty = difficulty;

            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
