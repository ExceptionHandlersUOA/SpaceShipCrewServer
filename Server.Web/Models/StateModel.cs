using Server.Web.Enums;
using Server.Web.Extensions;
using Server.Web.World;
using System.Text.Json.Serialization;

namespace Server.Web.Models;
public class StateModel
{
    [JsonIgnore]
    public Dictionary<Role, PlayerState> InternalRoles { get; set; }

    [JsonIgnore]
    public GameState InternalGameState { get; set; }

    public string CurrentSequence { get; set; }

    public string GameState {
        get => GameStateExt.ToString(InternalGameState);
        set => InternalGameState = GameStateExt.ToGameState(value);
    }

    public Dictionary<int, RoleModel> Roles
    {
        get => InternalRoles.ToDictionary(x => x.Value.PlayerId, x =>
            new RoleModel()
            {
                RoleId = RoleExt.ToString(x.Key),
                UserId = x.Value.PlayerId,
                Username = x.Value.Username
            }
        );
        set => throw new ArgumentException("Cannot set roles via JSON deserialization!");
    }

    public SendResourceModel Resources { get; set; }
}
