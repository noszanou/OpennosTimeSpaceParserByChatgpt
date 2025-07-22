using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class GpPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("gp ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static GpPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid GP packet");

            var packet = new GpPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 6)
                    throw new ArgumentException($"GP packet too short. Expected at least 6 parts, got {parts.Length}");

                packet.SourceX = int.Parse(parts[1]);
                packet.SourceY = int.Parse(parts[2]);
                packet.DestinationMapId = int.Parse(parts[3]);
                packet.Type = int.Parse(parts[4]);
                packet.PortalId = int.Parse(parts[5]);

                if (parts.Length > 6)
                    packet.IsDisabled = int.Parse(parts[6]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing GP packet: {ex.Message}", ex);
            }
        }
    }
}