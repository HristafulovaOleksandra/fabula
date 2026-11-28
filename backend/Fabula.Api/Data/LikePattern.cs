namespace Fabula.Api.Data;

public static class LikePattern
{
    public const string Escape = @"\";

    public static string Contains(string text) =>
        "%" + text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
