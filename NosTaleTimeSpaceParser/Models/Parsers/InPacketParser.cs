using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class InPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("in ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static InPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid IN packet");

            var packet = new InPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 8)
                    throw new ArgumentException($"IN packet too short. Expected at least 8 parts, got {parts.Length}");

                packet.Type = int.Parse(parts[1]);

                // Pour les objets (type 9), le parsing est différent
                if (packet.Type == 9)
                {
                    ParseObjectPacket(packet, parts);
                }
                else
                {
                    ParseEntityPacket(packet, parts);
                }

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing IN packet: {ex.Message}", ex);
            }
        }

        private static void ParseObjectPacket(InPacket packet, string[] parts)
        {
            // Format: in 9 VNum ID X Y Direction 0 0 -1
            if (parts.Length >= 9)
            {
                packet.VNum = int.Parse(parts[2]);
                packet.EntityId = int.Parse(parts[3]);
                packet.PositionX = int.Parse(parts[4]);
                packet.PositionY = int.Parse(parts[5]);
                packet.Direction = int.Parse(parts[6]);
                packet.Name = $"Object_{packet.VNum}";
            }
        }

        private static void ParseEntityPacket(InPacket packet, string[] parts)
        {
            // Format complexe pour NPCs/Players/Monsters
            // Essayer de parser les champs principaux

            // Le nom peut être composé de plusieurs parties (ex: "SEOVA - ")
            int nameEndIndex = FindNameEndIndex(parts, 2);

            if (nameEndIndex > 2)
            {
                packet.Name = string.Join(" ", parts, 2, nameEndIndex - 2).Trim();
            }

            // Les indices peuvent varier selon la longueur du nom
            int dataStartIndex = nameEndIndex;

            if (parts.Length > dataStartIndex + 5)
            {
                if (int.TryParse(parts[dataStartIndex], out int vnum))
                    packet.VNum = vnum;

                if (int.TryParse(parts[dataStartIndex + 1], out int entityId))
                    packet.EntityId = entityId;

                if (int.TryParse(parts[dataStartIndex + 2], out int x))
                    packet.PositionX = x;

                if (int.TryParse(parts[dataStartIndex + 3], out int y))
                    packet.PositionY = y;

                if (int.TryParse(parts[dataStartIndex + 4], out int direction))
                    packet.Direction = direction;

                if (parts.Length > dataStartIndex + 5 && int.TryParse(parts[dataStartIndex + 5], out int hp))
                    packet.CurrentHp = hp;

                if (parts.Length > dataStartIndex + 6 && int.TryParse(parts[dataStartIndex + 6], out int mp))
                    packet.CurrentMp = mp;

                if (parts.Length > dataStartIndex + 7 && int.TryParse(parts[dataStartIndex + 7], out int dialog))
                    packet.Dialog = dialog;
            }

            // Stocker les données restantes
            if (parts.Length > dataStartIndex + 8)
            {
                for (int i = dataStartIndex + 8; i < parts.Length; i++)
                {
                    packet.AdditionalData.Add(parts[i]);
                }
            }
        }

        private static int FindNameEndIndex(string[] parts, int startIndex)
        {
            // Chercher où se termine le nom en trouvant le premier nombre
            for (int i = startIndex; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out _))
                {
                    return i;
                }
            }
            return Math.Min(startIndex + 3, parts.Length); // Fallback
        }
    }
}