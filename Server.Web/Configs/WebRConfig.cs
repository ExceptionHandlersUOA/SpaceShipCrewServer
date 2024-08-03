using Server.Base.Core.Abstractions;

namespace Server.Web.Configs;

public class WebRConfig : IRConfig
{
    public int MaxPlayersPerGame = 3;
    public int InviteLength = 4;
}
