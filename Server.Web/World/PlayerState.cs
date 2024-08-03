using Server.Web.Enums;
using Server.Web.Protocols;

namespace Server.Web.World;

public class PlayerState
{
    public int PlayerId { get; set; }
    public string Username { get; set; }
    public required IGamePlayer Proxy { get; init; }
    public Role Role { get; set; }
}
