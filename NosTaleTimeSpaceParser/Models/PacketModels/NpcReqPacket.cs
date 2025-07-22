namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class NpcReqPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Owner { get; set; }
        public int DialogId { get; set; }

        public override string ToString()
        {
            return $"NPC_REQ - Type: {Type}, Owner: {Owner}, DialogId: {DialogId}";
        }
    }
}