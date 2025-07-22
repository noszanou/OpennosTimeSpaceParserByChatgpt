namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class SuPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public int CallerId { get; set; }
        public int SecondaryType { get; set; }
        public int TargetId { get; set; }
        public int SkillVNum { get; set; }
        public int Cooldown { get; set; }
        public int AttackAnimation { get; set; }
        public int SkillEffect { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int IsAlive { get; set; }
        public int Health { get; set; }
        public int Damage { get; set; }
        public int HitMode { get; set; }
        public int SkillType { get; set; }

        public SuActionType ActionType => Type switch
        {
            1 => SuActionType.PlayerSkill,
            2 => SuActionType.MonsterSkill,
            3 => SuActionType.MonsterSpawn,
            _ => SuActionType.Unknown
        };

        public override string ToString()
        {
            return $"SU - Action: {ActionType}, Caller: {CallerId}, Target: {TargetId}, Position: ({PositionX},{PositionY}), Skill: {SkillVNum}";
        }
    }

    public enum SuActionType
    {
        Unknown,
        PlayerSkill,
        MonsterSkill,
        MonsterSpawn
    }
}