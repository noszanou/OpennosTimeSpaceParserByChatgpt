using System.Globalization;
using NosTaleTimeSpaceParser.Models.PacketModels;

namespace NosTaleTimeSpaceParser.Parsers
{
    public class RbrPacketParser
    {
        public static bool CanParse(string packetLine)
        {
            return packetLine?.Trim().StartsWith("rbr ", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static RbrPacket Parse(string packetLine)
        {
            if (!CanParse(packetLine))
                throw new ArgumentException("Invalid RBR packet");

            var packet = new RbrPacket { RawPacket = packetLine };

            try
            {
                var parts = packetLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 20)
                    throw new ArgumentException($"RBR packet too short. Expected at least 20 parts, got {parts.Length}");

                packet.Version = parts[1];
                packet.FixedValue1 = int.Parse(parts[2]);
                packet.FixedValue2 = int.Parse(parts[3]);

                var levelParts = parts[4].Split('.');
                if (levelParts.Length >= 2)
                {
                    if (int.TryParse(levelParts[0], out int minLevel))
                        packet.LevelMinimum = minLevel;
                    if (int.TryParse(levelParts[1], out int maxLevel))
                        packet.LevelMaximum = maxLevel;
                }

                packet.RequiredItemsAmount = int.Parse(parts[5]);
                packet.DrawItems = ParseItemList(parts, 6, 10);
                packet.SpecialItems = ParseItemList(parts, 11, 12);
                packet.GiftItems = ParseItemList(parts, 13, 15);

                if (parts.Length > 16)
                {
                    var scoreParts = parts[16].Split('.');
                    if (scoreParts.Length >= 1)
                    {
                        if (float.TryParse(scoreParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float winnerScore))
                            packet.WinnerScore = winnerScore;
                    }
                }

                if (parts.Length > 17 && int.TryParse(parts[17], out int winner))
                    packet.Winner = winner;

                if (parts.Length > 18 && int.TryParse(parts[18], out int scoreValue))
                    packet.ScoreValue = scoreValue;

                ParseNameAndLabel(packetLine, packet);

                return packet;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing RBR packet: {ex.Message}", ex);
            }
        }

        private static List<RbrPacket.ItemInfo> ParseItemList(string[] parts, int startIndex, int endIndex)
        {
            var items = new List<RbrPacket.ItemInfo>();

            for (int i = startIndex; i <= endIndex && i < parts.Length; i++)
            {
                if (parts[i] == "-1.0" || parts[i] == "-1" || string.IsNullOrEmpty(parts[i]))
                    continue;

                var itemParts = parts[i].Split('.');
                if (itemParts.Length >= 2)
                {
                    if (int.TryParse(itemParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int vnum) &&
                        int.TryParse(itemParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                    {
                        if (vnum > 0)
                        {
                            items.Add(new RbrPacket.ItemInfo(vnum, amount));
                        }
                    }
                }
            }

            return items;
        }

        private static void ParseNameAndLabel(string packetLine, RbrPacket packet)
        {
            // Après les parties fixes, chercher "Tutorial" qui est le début du nom
            var parts = packetLine.Split(' ');

            // Trouver l'index où commence le vrai nom (après les scores "0. 0 0")
            int nameStartIndex = -1;
            for (int i = 16; i < parts.Length; i++)
            {
                // Le nom commence après les 3 derniers scores (0. 0 0)
                // Chercher la première partie qui n'est pas un nombre
                if (!IsNumericOrScore(parts[i]))
                {
                    nameStartIndex = i;
                    break;
                }
            }

            if (nameStartIndex == -1) return;

            var nameParts = new List<string>();
            var descriptionParts = new List<string>();
            bool foundDescription = false;

            for (int i = nameStartIndex; i < parts.Length; i++)
            {
                if (parts[i].Contains('\n'))
                {
                    var splitPart = parts[i].Split('\n', 2);
                    nameParts.Add(splitPart[0]);

                    if (splitPart.Length > 1)
                    {
                        foundDescription = true;
                        if (!string.IsNullOrEmpty(splitPart[1]))
                            descriptionParts.Add(splitPart[1]);
                    }
                }
                else if (!foundDescription)
                {
                    nameParts.Add(parts[i]);
                }
                else
                {
                    descriptionParts.Add(parts[i]);
                }
            }

            packet.Name = string.Join(" ", nameParts).Trim();
            packet.Label = string.Join(" ", descriptionParts).Trim();
        }

        private static bool IsNumericOrScore(string part)
        {
            return part == "0" || part == "0." ||
                   int.TryParse(part, out _) ||
                   float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}