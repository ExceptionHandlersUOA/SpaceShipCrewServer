namespace Server.Web.Models;
public class SendResourceModel
{
    private readonly ResourcesModel _model;

    public SendResourceModel(ResourcesModel model) => _model = model;

    public SendResourceModel() { }

    public int Oxygen { get => (int)Math.Floor(_model.Oxygen); set => _model.Oxygen = value; }
    public int Electricity { get => (int)Math.Floor(_model.Electricity); set => _model.Electricity = value; }
    public int Fuel { get => (int)Math.Floor(_model.Fuel); set => _model.Fuel = value; }
    public int Water { get => (int)Math.Floor(_model.Water); set => _model.Water = value; }
}
