namespace Server.Web.Models;
public class ResourcesModel
{
    public int Oxygen { get; set; } = UpperBounds;
    public int Electricity { get; set; } = UpperBounds;
    public int Fuel { get; set; } = UpperBounds;
    public int Water { get; set; } = UpperBounds;

    public const int UpperBounds = 100;

    public bool Depleated() =>
        Oxygen <= 0 || Electricity <= 0 || Fuel <= 0 || Water <= 0;

    public void EnsureBounds()
    {
        if (Oxygen > UpperBounds)
            Oxygen = UpperBounds;

        if (Fuel > UpperBounds)
            Fuel = UpperBounds;

        if (Water > UpperBounds)
            Water = UpperBounds;

        if (Electricity > UpperBounds)
            Electricity = UpperBounds;
    }
}
