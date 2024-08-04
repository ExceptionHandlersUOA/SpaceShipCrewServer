using System.Drawing;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Server.Web.Models;
public class MessageModel
{
    public MessageModel() { }

    public MessageModel(string message, Color color)
    {
        Message = message;
        InternalColor = color;
    }

    public string Message { get; set; }
    public string ColorHex { get => HexConverter(InternalColor); set => InternalColor = Color.FromArgb(int.Parse(value.Replace("#", ""), NumberStyles.HexNumber)); }

    [JsonIgnore]
    public Color InternalColor { get; set; }

    private static string HexConverter(Color c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
