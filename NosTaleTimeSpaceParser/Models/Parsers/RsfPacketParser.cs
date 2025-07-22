using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class RsfPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            var trimmed = packetLine?.Trim();
            return trimmed?.StartsWith("rsfn ", StringComparison.OrdinalIgnoreCase) == true ||
                   trimmed?.StartsWith("rsfm ", StringComparison.OrdinalIgnoreCase) == true ||
                   trimmed?.StartsWith("rsfp ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static RsfPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid RSF packet");

            var packet = new RsfPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    throw new ArgumentException($"RSF packet too short. Expected at least 2 parts, got {parts.Length}");

                packet.Type = parts[0];

                for (int i = 1; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i], out int value))
                        packet.Values.Add(value);
                }

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing RSF packet: {ex.Message}", ex);
            }
        }
    }
}