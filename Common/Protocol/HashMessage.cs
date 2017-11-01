namespace Common
{
    public class HashMessage : ProtocolMessage
    {
        public HashMessage(string file, long start, int length, string hash, string content)
            : base(ProtocolCommand.HASH)
        {
            File = file;
            Start = start;
            Length = length;
            Hash = hash;
            Content = content;
        }

        public string File { get; }

        public long Start { get; }

        public int Length { get; }

        public string Hash { get; }

        public string Content { get; }

        public override string ToString()
        {
            return $"{Command}|{File}|{Start}|{Length}|{Hash}|{Content}";
        }
    }
}
