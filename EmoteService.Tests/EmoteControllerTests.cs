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
    public async Task EmoteManipulation_GetAddResponse()
    {
        // Arrange
        var application = new EmoteServiceWebApplicationFactory();
        var targetEmotes = new List<string>() { "shikanokonokonokokoshitantan" };
        var targetEmotes_remove = new List<string>() { "test" };

        var clientinfo = new ClientInfo() {username = "bassnix", channel = "bassnixbot", message = ""};
        
        var request_add = new ModifyEmoteinEmoteSetRequest() { source = "bassnix", targetemotes=targetEmotes, clientinfo = clientinfo};
        var request_rename = new ModifyEmoteinEmoteSetRequest() { targetemotes=targetEmotes, clientinfo = clientinfo, emoterename = "test"};
        var request_remove = new ModifyEmoteinEmoteSetRequest() { source = "bassnix", targetemotes=targetEmotes_remove, clientinfo = clientinfo};

        var client = application.CreateClient();

        // Act
        var response_add = await client.PostAsJsonAsync("emotes/add", request_add);
        await Task.Delay(TimeSpan.FromSeconds(5));
        var response_rename = await client.PostAsJsonAsync("emotes/rename", request_rename);
        await Task.Delay(TimeSpan.FromSeconds(5));
        var response_remove = await client.PostAsJsonAsync("emotes/remove", request_remove);

        // Assert
        response_add.EnsureSuccessStatusCode();

        var contentResponse_add = await response_add.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Equal(
            "| Successfully added this emote(s): shikanokonokonokokoshitantan | ",
            contentResponse_add.result
        );

        response_rename.EnsureSuccessStatusCode();

        var contentResponse_rename = await response_rename.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Equal(
            "| Succcessfully rename shikanokonokonokokoshitantan to test | ",
            contentResponse_rename.result
        );

        response_remove.EnsureSuccessStatusCode();

        var contentResponse_remove= await response_remove.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.Equal(
            "| Successfully removed the emote(s): test | ",
            contentResponse_remove.result
        );
        
    }
}
