using Server.Web.Extensions;

namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM MOBILE
    public void ActionButton(ActionButtonDTO data)
    {
        // A button was pressed/unpressed, the ID of which is attached

        var action = ActionExt.ToAction(data.ActionId);
        var direction = DirectionExt.ToDirection(data.Direction);
    }

    public class ActionButtonDTO
    {
        public string ActionId { get; set; }
        public string Direction { get; set; }
    }
}
