using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class EvntPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("evnt ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static EvntPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid EVNT packet");

            var packet = new EvntPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 5)
                    throw new ArgumentException($"EVNT packet too short. Expected at least 5 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);
                packet.Unknown1 = int.Parse(parts[2]);
                packet.Time1 = int.Parse(parts[3]);
                packet.Time2 = int.Parse(parts[4]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing EVNT packet: {ex.Message}", ex);
            }
        }
    }
}