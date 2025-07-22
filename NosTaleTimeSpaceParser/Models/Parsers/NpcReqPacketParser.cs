using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class NpcReqPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("npc_req ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static NpcReqPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid NPC_REQ packet");

            var packet = new NpcReqPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                    throw new ArgumentException($"NPC_REQ packet too short. Expected at least 4 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);
                packet.Owner = int.Parse(parts[2]);
                packet.DialogId = int.Parse(parts[3]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing NPC_REQ packet: {ex.Message}", ex);
            }
        }
    }
}