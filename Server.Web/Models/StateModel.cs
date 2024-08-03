using Server.Web.Enums;
using Server.Web.Extensions;
using Server.Web.World;
using System.Text.Json.Serialization;

namespace Server.Web.Models;
public class StateModel
{
    [JsonIgnore]
    public Dictionary<Role, PlayerState> InternalRoles { get; set; }

    public Dictionary<int, string> Roles {
        get => InternalRoles.ToDictionary(x => x.Value.PlayerId, x => RoleExt.ToString(x.Key));
        set => InternalRoles = value.ToDictionary(x => RoleExt.ToRole(x.Value), x => new PlayerState() { PlayerId = x.Key, Proxy = null });
    }

    public ResourcesModel Resources { get; set; }
}
