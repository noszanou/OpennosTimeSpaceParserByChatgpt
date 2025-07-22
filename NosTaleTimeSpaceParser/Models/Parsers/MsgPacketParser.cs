using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class MsgPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("msg ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static MsgPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid MSG packet");

            var packet = new MsgPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', 3, StringSplitOptions.None);

                if (parts.Length < 3)
                    throw new ArgumentException($"MSG packet too short. Expected at least 3 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);
                packet.Message = parts[2].Trim();

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing MSG packet: {ex.Message}", ex);
            }
        }
    }
}