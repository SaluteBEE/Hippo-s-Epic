
public static class LevelManager
{
    private static Level level;
    public static Level Level
    {
        get
        {
            if (level == null)
            {
                throw new System.Exception("LevelManager.Level is not set. Please set it before accessing.");
            }
            return level;
        }
        set
        {
            if (level != null)
            {
                throw new System.Exception("LevelManager.Level is already set. Multiple assignments are not allowed.");
            }
            level = value;
        }
    }

    public static bool SetLevel(Level level)
    {
        if (Level != null)
        {
            throw new System.Exception("LevelManager.Level is already set. Multiple assignments are not allowed.");
        }
        Level = level;
        return true;
    }

    public static void ClearLevel()
    {
        level = null;
    }
}