namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class RsfPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<int> Values { get; set; } = new List<int>();

        public RsfType RsfType => Type.ToLower() switch
        {
            "rsfn" => RsfType.Node,
            "rsfm" => RsfType.Map,
            "rsfp" => RsfType.Position,
            _ => RsfType.Unknown
        };

        public override string ToString()
        {
            return $"RSF - Type: {RsfType}, Values: [{string.Join(",", Values)}]";
        }
    }

    public enum RsfType
    {
        Unknown,
        Node,
        Map,
        Position
    }
}