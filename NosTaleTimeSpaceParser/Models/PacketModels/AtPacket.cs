namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class AtPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int MapId { get; set; }
        public int GridMapId { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Direction { get; set; }
        public int Unknown1 { get; set; }
        public int InstanceMusic { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }

        public override string ToString()
        {
            return $"AT - MapId: {MapId}, GridMapId: {GridMapId}, Position: ({PositionX},{PositionY}), Music: {InstanceMusic}";
        }
    }
}