using Server.Web.Models;

namespace Server.Web.Protocols;

public interface IGamePlayer
{
    // Debug messages are sent to the client.
    Task WriteMessage(string message);

    // Not all players have picked a role or it does not meet player count.
    Task GameNotReady();

    // All players have a role and meet maximum player count.
    Task GameReady();

    // Tutorial introduction animation begins.
    // Once it ends, the first state will be sent.
    Task TutorialStart();

    // Sent to all clients when the tutorial has finished and the game starts.
    Task GameStart();

    // State has been updated, new state is sent to client
    Task State(StateModel state);

    // Sent when all resources are depleted
    Task GameEnd();
}
