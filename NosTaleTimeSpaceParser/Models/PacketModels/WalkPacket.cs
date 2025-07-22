namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class WalkPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Unknown1 { get; set; }
        public int Speed { get; set; }

        public override string ToString()
        {
            return $"WALK - Position: ({PositionX},{PositionY}), Speed: {Speed}";
        }
    }
}