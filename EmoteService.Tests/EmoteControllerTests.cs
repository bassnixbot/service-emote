using System.Net.Http.Json;
using System.Text.Json;
using EmoteService.Models;
using UtilsLib;

namespace EmoteService.Tests;

public class EmoteControllerTests
{
    [Fact]
    public async Task PreviewEmote_GetPreviewLink()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var targetEmotes = new List<string>() { "wha" };
        var request = new PreviewRequest() { targetemotes = targetEmotes, source = "bassnix" };

        var client = application.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("emotes/preview", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "| wha - ( https://cdn.7tv.app/emote/642839073ff2b562db16cad2/4x.webp ) |",
            contentResponse.result
        );
    }

    [Fact]
    public async Task SearchEmote_GetEmoteString()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var queryparams = new List<string> { "channel=bassnixbot", "query=ta" };
        var rooturl = "emotes/searchemotes";

        var targetUrl = $"{rooturl}?{string.Join("&", queryparams)}";

        Console.WriteLine(targetUrl);
        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync(targetUrl);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        Assert.Contains(
            "Stare tank",
            string.Join(" ", contentResponse.result) 
        );
    }
    
    [Fact]
    public async Task GetChannelEditors_GetEditorsName()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var queryparams = new List<string> { "user=bassnixbot" };
        var rooturl = "emotes/getchanneleditors";

        var targetUrl = $"{rooturl}?{string.Join("&", queryparams)}";

        Console.WriteLine(targetUrl);
        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync(targetUrl);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        Assert.Contains(
            "bassnix",
            string.Join(" ", contentResponse.result) 
        );
    }

    [Fact]
    public async Task GetUserEditorAccess_GetChannelAccess()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var queryparams = new List<string> { "user=bassnixbot" };
        var rooturl = "emotes/getusereditoraccess";

        var targetUrl = $"{rooturl}?{string.Join("&", queryparams)}";

        Console.WriteLine(targetUrl);
        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync(targetUrl);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        Assert.Contains(
            "bassnix",
            string.Join(" ", contentResponse.result) 
        );
    }
    
    [Fact]
    public async Task AddEmote_GetAddResponse()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var targetEmotes = new List<string>() { "shikanokonokonokokoshitantan" };
        var request = new ModifyEmoteinEmoteSetRequest() { source = "bassnix", targetemotes=targetEmotes, targetchannel = "bassnixbot"};

        var client = application.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("emotes/add", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "shikanokonokonokokoshitantan",
            contentResponse.result
        );

        // remove the emote
        request.source = null;
        var removeResponse = await client.PostAsJsonAsync("emotes/remove", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var removeContentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "shikanokonokonokokoshitantan",
            contentResponse.result
        );
    }
    
    [Fact]
    public async Task RenameEmote_GetRenameResponse()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var targetEmotes = new List<string>() { "catSigh" };
        var addRequest = new ModifyEmoteinEmoteSetRequest() { source = "bassnix", targetemotes=targetEmotes, targetchannel = "bassnixbot"};

        var client = application.CreateClient();

        // Act
        var addResponse = await client.PostAsJsonAsync("emotes/add", addRequest);

        // Assert
        addResponse.EnsureSuccessStatusCode();

        var addContentResponse = await addResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "shikanokonokonokokoshitantan",
            addContentResponse.result
        );

        // Arrange
        var request = new ModifyEmoteinEmoteSetRequest() { targetemotes=targetEmotes, targetchannel = "bassnixbot", emoterename = "test"};

        var client = application.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("emotes/rename", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "test",
            contentResponse.result
        );
    }

    [Fact]
    public async Task RemoveEmote_GetRemoveResponse()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var targetEmotes = new List<string>() { "test" };
        var request = new ModifyEmoteinEmoteSetRequest() { targetemotes=targetEmotes, targetchannel = "bassnixbot"};

        var client = application.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("emotes/remove", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var contentResponse = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Contains(
            "test",
            contentResponse.result
        );
    }
}
