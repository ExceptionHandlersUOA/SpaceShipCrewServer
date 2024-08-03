namespace Server.Web.World
{
    public interface IGamePlayer
    {
        Task WriteMessage(string message);
        Task GameStarted();
        Task GameCompleted();
    }
}
