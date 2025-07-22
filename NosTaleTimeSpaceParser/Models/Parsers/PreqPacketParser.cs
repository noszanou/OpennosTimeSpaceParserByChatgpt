using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class PreqPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().Equals("preq", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static PreqPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid PREQ packet");

            return new PreqPacket { RawPacket = packetLine };
        }
    }
}