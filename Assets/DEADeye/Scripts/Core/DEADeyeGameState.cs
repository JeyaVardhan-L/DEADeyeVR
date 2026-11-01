namespace DEADeye
{
    /// <summary>
    /// Authoritative high-level lifecycle state for DEADeye VR.
    /// </summary>
    public enum DEADeyeGameState
    {
        Boot,
        Lobby,
        MissionBriefing,
        MissionActive,
        MissionPaused,
        MissionResults
    }

    /// <summary>
    /// Mission difficulty settings modifying hostile reaction speed, health, and spawn pressure.
    /// </summary>
    public enum MissionDifficulty
    {
        Recruit,
        Operative,
        Elite
    }
}

