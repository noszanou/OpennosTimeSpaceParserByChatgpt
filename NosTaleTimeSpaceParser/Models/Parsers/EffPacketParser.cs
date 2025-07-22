using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class EffPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("eff ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static EffPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid EFF packet");

            var packet = new EffPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                    throw new ArgumentException($"EFF packet too short. Expected at least 4 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);
                packet.EntityId = int.Parse(parts[2]);
                packet.EffectId = int.Parse(parts[3]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing EFF packet: {ex.Message}", ex);
            }
        }
    }
}