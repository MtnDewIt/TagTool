using System;

namespace TagTool.Common
{
    public enum GameDifficulty : short
    {
        Easy,
        Normal,
        Heroic,
        Legendary
    }

    [Flags]
    public enum GameDifficultyFlags : ushort
    {
        None = 0,
        Easy = 1 << 0,
        Normal = 1 << 1,
        Heroic = 1 << 2,
        Legendary = 1 << 3
    }
}