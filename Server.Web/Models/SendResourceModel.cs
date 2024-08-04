namespace Server.Web.Models;
public class ResourcesModel
{
    public float Oxygen { get; set; } = UpperBounds;
    public float Electricity { get; set; } = UpperBounds;
    public float Fuel { get; set; } = UpperBounds;
    public float Water { get; set; } = UpperBounds;

    public const float UpperBounds = 100;

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
