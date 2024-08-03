using Server.Web.Extensions;

namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM MOBILE
    public void ActionSwitch(ActionSwitchDTO data)
    {
        // A switch was toggled, the ID of which is attached

        var action = ActionExt.ToAction(data.ActionId);
        var enabled = data.Value;
    }

    public class ActionSwitchDTO
    {
        public string ActionId { get; set; }
        public bool Value { get; set; }
    }
}
