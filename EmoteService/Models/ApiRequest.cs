using UtilsLib;

namespace EmoteService.Models;

public class ModifyEmoteinEmoteSetRequest
{
    public string? source { get; set; }
    public string? owner { get; set; }
    public string emoterename { get; set; } = "";
    public List<string> targetemotes { get; set; } = new();
    public bool defaultname { get; set; } = false;
    public bool iscaseinsensitive { get; set; } = false;
    public ClientInfo clientinfo {get; set;}
}

public class PreviewRequest
{
    public List<string> targetemotes { get; set; } = new();
    public string? source { get; set; }
}
