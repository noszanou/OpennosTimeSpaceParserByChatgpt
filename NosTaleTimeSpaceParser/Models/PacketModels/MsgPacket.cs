namespace NosTaleTimeSpaceParser.Models.PacketModels
{
    public class MsgPacket
    {
        public string RawPacket { get; set; } = string.Empty;
        public int Type { get; set; }
        public string Message { get; set; } = string.Empty;

        public MessageType MessageType => Type switch
        {
            0 => MessageType.Normal,
            1 => MessageType.Whisper,
            2 => MessageType.System,
            3 => MessageType.Guild,
            4 => MessageType.Group,
            5 => MessageType.Broadcast,
            _ => MessageType.Unknown
        };

        public override string ToString()
        {
            return $"MSG - Type: {MessageType}, Message: \"{Message}\"";
        }
    }

    public enum MessageType
    {
        Unknown,
        Normal,
        Whisper,
        System,
        Guild,
        Group,
        Broadcast
    }
}