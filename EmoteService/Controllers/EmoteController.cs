using AutoMapper;
using EmoteService.GraphQl;
using EmoteService.Models;
using EmoteService.Services;
using FuzzySharp;
using Microsoft.AspNetCore.Mvc;
using UtilsLib;

namespace EmoteService.Controllers;

[ApiController]
[Route("emotes")]
public class EmoteController : ControllerBase
{
    private readonly ISevenTvClient _client;
    private readonly IMapper _mapper;
    private readonly ILogger<EmoteController> _logger;

    public EmoteController(ILogger<EmoteController> logger, ISevenTvClient client, IMapper mapper)
    {
        _logger = logger;
        _client = client;
        _mapper = mapper;
    }

    [HttpPost("preview")]
    public async Task<ActionResult> EmotePreview(PreviewRequest request)
    {
        var response = new ApiResponse<string> { success = false };

        var apiresponse = await EmoteServices.EmotePreview(request, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpGet("searchemotes")]
    public async Task<ActionResult> SearchEmotes(string channel, string? query, string? tags)
    {
        var response = new ApiResponse<string> { success = false };

        var apiresponse = await EmoteServices.SearchEmotes(channel, query, tags, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpGet("getchanneleditors")]
    public async Task<ActionResult> GetChannelEditors(string user)
    {
        var response = new ApiResponse<string> { success = false };

        var apiresponse = await EmoteServices.GetChannelEditors(user, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpGet("getusereditoraccess")]
    public async Task<ActionResult> GetUserEditorAccess(string user)
    {
        var response = new ApiResponse<string> { success = false };

        var apiresponse = await EmoteServices.GetChannelEditors(user, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddEmoteinEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string> { success = false };

        // var checkperms = await EmoteServices.CheckPerms(request.clientinfo, new ServiceDependency() {client = _client, mapper = _mapper});
        //
        // if (!checkperms.success) {
        //     return StatusCode(StatusCodes.Status500InternalServerError, checkperms);
        // }         

        var apiresponse = await EmoteServices.AddEmoteinEmoteSet(request, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpPost("remove")]
    public async Task<ActionResult> RemoveEmoteFromEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string> { success = false };
        
        var checkperms = await EmoteServices.CheckPerms(request.clientinfo, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!checkperms.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, checkperms);
        }         

        var apiresponse = await EmoteServices.RemoveEmoteFromEmoteSet(request, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }

    [HttpPost("rename")]
    public async Task<ActionResult> RenameEmoteFromEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string> { success = false };
        
        var checkperms = await EmoteServices.CheckPerms(request.clientinfo, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!checkperms.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, checkperms);
        }         

        var apiresponse = await EmoteServices.RenameEmoteFromEmoteSet(request, new ServiceDependency() {client = _client, mapper = _mapper});

        if (!apiresponse.success) {
            return StatusCode(StatusCodes.Status500InternalServerError, apiresponse);
        } else {
            return Ok(apiresponse);
        }
    }
}
