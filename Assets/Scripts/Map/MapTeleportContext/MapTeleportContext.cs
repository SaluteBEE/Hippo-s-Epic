namespace Map
{
    public readonly struct MapTeleportContext
    {
        public int TargetMapIndex { get; }
        public int TargetMapTeleportPositionIndex { get; }

        public MapTeleportContext(int targetMapIndex, int targetMapTeleportPositionIndex)
        {
            TargetMapIndex = targetMapIndex;
            TargetMapTeleportPositionIndex = targetMapTeleportPositionIndex;
        }
    }
}