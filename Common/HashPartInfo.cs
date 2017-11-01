namespace Common
{
    public class HashPartInfo
    {
        public HashPartInfo(string file, long start, int length)
        {
            File = file;
            Start = start;
            Length = length;
        }

        public string File { get; set;  }

        public long Start { get; set; }

        public int Length { get; set; }

        public bool Transmitted { get; set; }
    }
}
