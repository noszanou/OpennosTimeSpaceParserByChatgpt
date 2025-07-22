namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class RbrPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public string Version { get; set; } = "0.0.0";
        public int FixedValue1 { get; set; } = 4;
        public int FixedValue2 { get; set; } = 15;
        public int LevelMinimum { get; set; }
        public int LevelMaximum { get; set; }
        public int RequiredItemsAmount { get; set; }
        public List<ItemInfo> DrawItems { get; set; } = new List<ItemInfo>();
        public List<ItemInfo> SpecialItems { get; set; } = new List<ItemInfo>();
        public List<ItemInfo> GiftItems { get; set; } = new List<ItemInfo>();
        public float WinnerScore { get; set; }
        public int Winner { get; set; }
        public int ScoreValue { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public class ItemInfo
        {
            public int VNum { get; set; }
            public int Amount { get; set; }

            public ItemInfo(int vnum, int amount)
            {
                VNum = vnum;
                Amount = amount;
            }

            public ItemInfo() { }

            public override string ToString()
            {
                return $"VNum: {VNum}, Amount: {Amount}";
            }
        }

        public override string ToString()
        {
            return $"RBR - Name: {Name}, Levels: {LevelMinimum}-{LevelMaximum}, DrawItems: {DrawItems.Count}";
        }
    }
}