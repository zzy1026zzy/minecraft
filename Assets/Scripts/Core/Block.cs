namespace Minecraft.Core
{
    public struct Block
    {
        public ushort id;

        public bool IsAir => id == 0;
        public bool IsSolid => id != 0;

        public Block(ushort id)
        {
            this.id = id;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
