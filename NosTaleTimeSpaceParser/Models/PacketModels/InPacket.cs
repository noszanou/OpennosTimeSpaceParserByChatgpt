namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class InPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VNum { get; set; }
        public int EntityId { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Direction { get; set; }
        public int CurrentHp { get; set; }
        public int CurrentMp { get; set; }
        public int Dialog { get; set; }
        public string Equipment { get; set; } = string.Empty;
        public List<string> AdditionalData { get; set; } = new List<string>();

        public EntityType EntityType => Type switch
        {
            1 => EntityType.Player,
            2 => EntityType.Npc,
            3 => EntityType.Monster,
            9 => EntityType.Object,
            _ => EntityType.Unknown
        };

        public override string ToString()
        {
            return $"IN - Type: {EntityType}, VNum: {VNum}, ID: {EntityId}, Position: ({PositionX},{PositionY}), Name: {Name}";
        }
    }

    public enum EntityType
    {
        Unknown,
        Player,
        Npc,
        Monster,
        Object
    }
}