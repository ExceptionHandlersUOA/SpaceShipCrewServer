using Server.Web.Extensions;

namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM MOBILE
    public void ActionNumber(ActionNumberDTO data)
    {
        // Value can go from 0 to 1, or -1 to 1 (dependent on UI)

        var action = ActionExt.ToAction(data.ActionId);
        var num = data.Value;
    }

    public class ActionNumberDTO
    {
        public string ActionId { get; set; }
        public float Value { get; set; }
    }
}
