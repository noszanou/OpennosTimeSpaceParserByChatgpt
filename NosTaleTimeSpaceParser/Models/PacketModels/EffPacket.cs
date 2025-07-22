namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class EffPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public int EntityId { get; set; }
        public int EffectId { get; set; }

        public override string ToString()
        {
            return $"EFF - Type: {Type}, EntityId: {EntityId}, EffectId: {EffectId}";
        }
    }
}