namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class PreqPacket
    {
        public string RawPacket { get; set; } = string.Empty;

        public override string ToString()
        {
            return "PREQ - Prerequisite/Condition";
        }
    }
}