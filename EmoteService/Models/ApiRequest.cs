namespace EmoteService.Models;

public class ModifyEmoteinEmoteSetRequest
{
    public string? source { get; set; }
    public string? owner { get; set; }
    public string emoterename { get; set; } = "";
    public List<string> targetemotes { get; set; } = new();
    public string targetchannel { get; set; }
    public bool defaultname { get; set; } = false;
    public bool iscaseinsensitive { get; set; } = false;
}

public class PreviewRequest
{
    public List<string> targetemotes { get; set; } = new();
    public string? source { get; set; }
}
