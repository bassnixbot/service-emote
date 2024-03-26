namespace EmoteService.Models;

public class Emotes
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string Rename { get; set; } = "";
    public string? errorMessage { get; set; }

    public string PreviewString()
    {
        return $"{Name} - ( https://cdn.7tv.app/emote/{Id}/4x.webp )";
    }

    public string GetEmoteIdentifier()
    {
        if (!string.IsNullOrEmpty(Rename))
            return Rename;

        return Name ?? Id ?? "";
    }

    public string RenameString()
    {
        if (string.IsNullOrEmpty(Rename))
            return "Default Name";

        return Rename;
    }
}

public class FailedEmotes
{
    public string Name { get; set; }
    public string? Fuzzy { get; set; }
    public int? Score { get; set; }

    public override string ToString()
    {
        return $"{Name} => {Fuzzy} ({Score})";
    }
}
