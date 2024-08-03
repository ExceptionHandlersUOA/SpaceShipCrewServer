namespace Server.Web.Protocols;

public partial class GameHub
{
    // FROM TV
    public async Task<RoomCreateAckDTO> RoomCreate()
    {
        // Returns room code for the room that was created.

        var game = await lobby.AddControllerToGameAsync(Context);

        return new RoomCreateAckDTO()
        {
            RoomCode = game.RoomCode
        };
    }

    public class RoomCreateAckDTO
    {
        public string RoomCode { get; set; }
    }
}
