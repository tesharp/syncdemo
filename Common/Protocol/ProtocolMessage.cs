namespace Common
{
    public abstract class ProtocolMessage
    {
        public ProtocolMessage(ProtocolCommand command)
        {
            Command = command;
        }

        public ProtocolCommand Command { get; }
    }
}
