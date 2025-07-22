using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class AtPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("at ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static AtPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid AT packet");

            var packet = new AtPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 9)
                    throw new ArgumentException($"AT packet too short. Expected at least 9 parts, got {parts.Length}");

                packet.MapId = int.Parse(parts[1]);
                packet.GridMapId = int.Parse(parts[2]);
                packet.PositionX = int.Parse(parts[3]);
                packet.PositionY = int.Parse(parts[4]);
                packet.Direction = int.Parse(parts[5]);
                packet.Unknown1 = int.Parse(parts[6]);
                packet.InstanceMusic = int.Parse(parts[7]);
                packet.Unknown2 = int.Parse(parts[8]);

                if (parts.Length > 9)
                    packet.Unknown3 = int.Parse(parts[9]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing AT packet: {ex.Message}", ex);
            }
        }
    }
}