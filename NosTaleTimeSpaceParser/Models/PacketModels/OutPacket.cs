namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class OutPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public int EntityId { get; set; }

        public override string ToString()
        {
            return $"OUT - Type: {Type}, EntityId: {EntityId}";
        }
    }
}