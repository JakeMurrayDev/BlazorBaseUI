using System.Globalization;

namespace BlazorBaseUI.Utilities.Direction;

public class DirectionService : IDirectionService
{
    private readonly CultureInfo culture;

    public DirectionService() : this(CultureInfo.CurrentUICulture)
    {
    }

    public DirectionService(CultureInfo culture)
    {
        this.culture = culture;
    }

    public TextDirection Direction
    {
        get
        {
            var textInfo = culture.TextInfo;
            return textInfo.IsRightToLeft ? TextDirection.Rtl : TextDirection.Ltr;
        }
    }

    public CultureInfo Culture => culture;
}

public interface IDirectionService
{
    TextDirection Direction { get; }
    CultureInfo Culture { get; }
}