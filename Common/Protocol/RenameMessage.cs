namespace Common
{
    public class RenameMessage : ProtocolMessage
    {
        public RenameMessage(string file, string newName)
            : base(ProtocolCommand.REN)
        {
            File = file;
            NewName = newName;
        }

        public string File { get; }

        public string NewName { get; }

        public override string ToString()
        {
            return $"{Command}|{File}|{NewName}";
        }
    }
}
