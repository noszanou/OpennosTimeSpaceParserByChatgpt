using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class WalkPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("walk ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static WalkPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid WALK packet");

            var packet = new WalkPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 5)
                    throw new ArgumentException($"WALK packet too short. Expected at least 5 parts, got {parts.Length}");

                packet.PositionX = int.Parse(parts[1]);
                packet.PositionY = int.Parse(parts[2]);
                packet.Unknown1 = int.Parse(parts[3]);
                packet.Speed = int.Parse(parts[4]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing WALK packet: {ex.Message}", ex);
            }
        }
    }
}