namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class EvntPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Unknown1 { get; set; }
        public int Time1 { get; set; }
        public int Time2 { get; set; }

        public override string ToString()
        {
            return $"EVNT - Type: {Type}, Times: {Time1}/{Time2}";
        }
    }
}