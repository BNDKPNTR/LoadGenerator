namespace LoadGenerator.Metrics
{
    struct GCCollectionCount
    {
        public int Gen0 { get; }
        public int Gen1 { get; }
        public int Gen2 { get; }

        public GCCollectionCount(int gen0, int gen1, int gen2)
        {
            Gen0 = gen0;
            Gen1 = gen1;
            Gen2 = gen2;
        }
    }
}
