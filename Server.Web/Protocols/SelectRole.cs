using Server.Web.Extensions;

namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM TV
    public async Task<SelectRoleAckDTO> SelectRole(SelectRoleDTO data)
    {
        // Tutorial animation has concluded
        // Start game timers and sending state
        var roleName = data.RoleName;

        var game = GetCurrentGame();

        if (game == null)
            return new SelectRoleAckDTO() { Success = false };

        var role = RoleExt.ToRole(roleName);

        if (game.IdToPlayer.Values.Any(x => x.Role == role))
            return new SelectRoleAckDTO() { Success = false };

        game.ConnectionToPlayer[Context.ConnectionId].Role = role;

        await game.CheckAndSendState();

        game.CheckGameReady();

        return new SelectRoleAckDTO() { Success = true };
    }

    public class SelectRoleAckDTO
    {
        public bool Success { get; set; }
    }

    public class SelectRoleDTO
    {
        public string RoleName { get; set; }
    }
}
