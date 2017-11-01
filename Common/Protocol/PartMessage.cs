namespace Common
{
    public class PartMessage : ProtocolMessage
    {
        public PartMessage(string file, long start, int length, string hash)
            : base(ProtocolCommand.PART)
        {
            File = file;
            Start = start;
            Length = length;
            Hash = hash;
        }

        public string File { get; }

        public long Start { get; }

        public int Length { get; }

        public string Hash { get; }

        public override string ToString()
        {
            return $"{Command}|{File}|{Start}|{Length}|{Hash}";
        }
    }
}
