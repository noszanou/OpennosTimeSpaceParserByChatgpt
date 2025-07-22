using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class SuPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("su ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static SuPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid SU packet");

            var packet = new SuPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 15)
                    throw new ArgumentException($"SU packet too short. Expected at least 15 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);
                packet.CallerId = int.Parse(parts[2]);
                packet.SecondaryType = int.Parse(parts[3]);
                packet.TargetId = int.Parse(parts[4]);
                packet.SkillVNum = int.Parse(parts[5]);
                packet.Cooldown = int.Parse(parts[6]);
                packet.AttackAnimation = int.Parse(parts[7]);
                packet.SkillEffect = int.Parse(parts[8]);
                packet.PositionX = int.Parse(parts[9]);
                packet.PositionY = int.Parse(parts[10]);
                packet.IsAlive = int.Parse(parts[11]);
                packet.Health = int.Parse(parts[12]);
                packet.Damage = int.Parse(parts[13]);
                packet.HitMode = int.Parse(parts[14]);

                if (parts.Length > 15)
                    packet.SkillType = int.Parse(parts[15]);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing SU packet: {ex.Message}", ex);
            }
        }
    }
}