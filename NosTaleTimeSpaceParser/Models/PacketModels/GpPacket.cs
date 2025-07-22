namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class GpPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int SourceX { get; set; }
        public int SourceY { get; set; }
        public int DestinationMapId { get; set; }
        public int Type { get; set; }
        public int PortalId { get; set; }
        public int IsDisabled { get; set; }

        public bool IsPortalDisabled => IsDisabled == 1;

        public override string ToString()
        {
            return $"GP - Position: ({SourceX},{SourceY}), DestMap: {DestinationMapId}, Type: {Type}, ID: {PortalId}, Disabled: {IsPortalDisabled}";
        }
    }
}