namespace Common
{
    public class DeleteMessage : ProtocolMessage
    {
        public DeleteMessage(string file)
            : base(ProtocolCommand.DEL)
        {
            File = file;
        }

        public string File { get; }

        public override string ToString()
        {
            return $"{Command}|{File}";
        }
    }
}
