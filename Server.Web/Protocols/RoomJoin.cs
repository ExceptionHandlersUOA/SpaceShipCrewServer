namespace Server.Web.Protocols;

public partial class GameHub
{
    // FROM MOBILE
    public async Task<RoomJoinAckDTO> RoomJoin(RoomJoinDTO data)
    {
        // Server returns room ID after joining the room.

        // UserId is 0 for TV,
        // >=1 for players.
        // Negative UserId indicates an invalid room code (not multicast)

        var roomCode = data.RoomCode;
        var username = data.Username;

        var id = await lobby.AddPlayerToGameAsync(Context, roomCode, username);

        return new RoomJoinAckDTO()
        {
            UserId = id
        };
    }

    public class RoomJoinDTO
    {
        public string RoomCode { get; set; }
        public string Username { get; set; }
    }

    public class RoomJoinAckDTO
    {
        public int UserId { get; set; }
    }
}
