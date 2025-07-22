using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class OutPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("out ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static OutPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid OUT packet");

            var packet = new OutPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                    packet.Type = int.Parse(parts[1]);

                if (parts.Length >= 3)
                    packet.EntityId = int.Parse(parts[2]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing OUT packet: {ex.Message}", ex);
            }
        }
    }
}