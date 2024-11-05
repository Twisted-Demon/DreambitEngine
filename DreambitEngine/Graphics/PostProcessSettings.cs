namespace Dreambit;

public class PostProcessSettings
{
    private static PostProcessSettings _instance;

    public static PostProcessSettings Instance => _instance ?? CreateNew();

    public static PostProcessSettings CreateNew()
    {
        var settings = new PostProcessSettings();
        
        _instance = settings;
        
        return _instance;
    }

    public float HueShift;
}