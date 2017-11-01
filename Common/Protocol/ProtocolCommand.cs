namespace Common
{
    public enum ProtocolCommand
    {
        DEL, // Delete
        REN, // Rename
        CHA, // Change
        PART, // Parts of a file, not content
        HASH, // Content of a hash
    }
}
