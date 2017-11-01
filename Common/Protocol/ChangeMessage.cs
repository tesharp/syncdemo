namespace Common
{
    public class ChangeMessage : ProtocolMessage
    {
        public ChangeMessage(string file, long size)
            : base(ProtocolCommand.CHA)
        {
            File = file;
            Size = size;
        }

        public string File { get; }

        public long Size { get; }

        public override string ToString()
        {
            return $"{Command}|{File}|{Size}";
        }
    }
}
