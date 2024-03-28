using AutoMapper;
using EmoteService.GraphQl;
using EmoteService.Models;
using EmoteService.Redis;
using EmoteService.Services;
using EmoteService.Singleton;
using FuzzySharp;
using Microsoft.AspNetCore.Mvc;

namespace EmoteService.Controllers;

[ApiController]
[Route("emotes")]
public class EmoteController : ControllerBase
{
    private readonly ISevenTvClient _client;
    private IRedisCache _redis;
    private readonly IMapper _mapper;
    private List<Error> _errorlist;

    private readonly ILogger<EmoteController> _logger;

    public EmoteController(
        ILogger<EmoteController> logger,
        IRedisCache redis,
        ISevenTvClient client,
        IMapper mapper
    )
    {
        _logger = logger;
        _redis = redis;
        _client = client;
        _mapper = mapper;
        var singletoncontainer = SingletonDataContainer.Instance;
        _errorlist = singletoncontainer.GetErrors();
    }

    [HttpPost("preview")]
    public async Task<ActionResult> EmotePreview(PreviewRequest request)
    {
        var response = new ApiResponse<string> { success = false };

        if (request.targetemotes.Count() == 0)
        {
            response.error = _errorlist.Where(x => x.errorCode == "7010").Single();
            return BadRequest(response);
        }

        EmoteServices.CheckObjectID(
            request.targetemotes,
            out List<Emotes> idlist,
            out List<Emotes> querylist
        );

        var preview_success = new List<Emotes>();
        var preview_failed = new List<Emotes>();

        if (idlist.Count() != 0)
        {
            foreach (var emoteid in idlist)
            {
                var getEmote = await EmoteServices.getEmote(_client, _redis, emoteid.Id.ToString());

                if (!getEmote.success)
                {
                    emoteid.errorMessage = getEmote!.error!.errorMessage;
                    preview_failed.Add(emoteid);
                    continue;
                }

                var emoteDetails = _mapper.Map<Emotes>(getEmote.result);

                preview_success.Add(emoteDetails);
            }
        }

        var preview_fuzzy = new List<FailedEmotes>();

        if (request.source != null && querylist.Count() != 0)
        {
            var getUserSourceId = await EmoteServices.queryUserId(
                _client,
                _redis,
                request.source
            );

            if (!getUserSourceId.success && getUserSourceId.result == null)
                return NotFound(getUserSourceId);

            var getUserSourceEmotes = await EmoteServices.getChannelEmotes(
                _client,
                _redis,
                getUserSourceId.result
            );

            if (!getUserSourceEmotes.success)
                return NotFound(getUserSourceEmotes);

            var sourceEmotes = _mapper.Map<List<Emotes>>(getUserSourceEmotes.result);

            var findEmotes = (
                from queries in querylist
                join emotesdict in sourceEmotes on queries.Name equals emotesdict.Name
                select new Emotes
                {
                    Name = emotesdict.Name,
                    Id = emotesdict.Id,
                    Rename = queries.Name
                }
            ).ToList();

            preview_success.AddRange(findEmotes);

            var emotes_notExactMatch = querylist
                .Select(x => x.Name)
                .Except(findEmotes.Select(x => x.Name))
                .Select(x => new Emotes { Name = x, errorMessage = "Emote Not Found" })
                .ToList();

            // serach for the nearest match for not exact match emotes
            var emoteNames = sourceEmotes.Select(x => x.Name).ToList();
            foreach (var emote in emotes_notExactMatch)
            {
                var fuzzyResult = Process.ExtractTop(emote.Name, emoteNames, cutoff: 70);

                if (!fuzzyResult.Any())
                    continue;

                preview_fuzzy.Add(
                    new FailedEmotes
                    {
                        Name = emote.Name,
                        Fuzzy = fuzzyResult.First().Value,
                        Score = fuzzyResult.First().Score
                    }
                );
            }

            // get the list of not found emotes
            var preview_notFound = emotes_notExactMatch
                .Select(x => x.Name)
                .Except(preview_fuzzy.Select(x => x.Name))
                .Select(x => new Emotes { Name = x })
                .ToList();

            preview_failed.AddRange(preview_notFound);
        }

        string actionmessage = "";
        if (preview_success.Any())
            actionmessage +=
                $" | {string.Join(" ", preview_success.Select(x => x.PreviewString()))} | ";

        if (preview_failed.Any())
            actionmessage += $" | {EmoteServices.EmoteErrorBuilder(preview_failed)} | ";

        if (preview_fuzzy.Any())
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", preview_fuzzy.Select(x => x.ToString()).ToList())} | ";

        response.success = true;
        response.result = actionmessage;

        return Ok(response);
    }

    [HttpGet("searchemotes")]
    public async Task<ActionResult> SearchEmotes(string channel, string? query, string? tags)
    {
        var getUserId = await EmoteServices.queryUserId(_client, _redis, channel);

        if (getUserId.success == false)
            return NotFound(getUserId);

        var emotelist = await EmoteServices.getChannelEmotes(_client, _redis, getUserId.result);

        if (!emotelist.success)
            return NotFound(emotelist);

        List<string> emotes = new();
        if (!string.IsNullOrEmpty(tags))
        {
            emotes = emotelist
                .result.Where(x => x.Data.Tags.Any(tag => tag.ToLower().Contains(tags.ToLower())))
                .Select(x => x.Name)
                .ToList();
            return Ok(new ApiResponse<List<string>> { success = true, result = emotes });
        }

        if (!string.IsNullOrEmpty(query))
        {
            emotes = emotelist
                .result.Where(x => x.Name.ToLower().Contains(query.ToLower() ?? ""))
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToList();
            return Ok(new ApiResponse<List<string>> { success = true, result = emotes });
        }

        // default fallback
        emotes = emotelist.result.Select(x => x.Name).ToList();
        return Ok(new ApiResponse<List<string>> { success = true, result = emotes });
    }

    [HttpGet("getchanneleditors")]
    public async Task<ActionResult> GetChannelEditors(string user)
    {
        // get user id
        var getUserId = await EmoteServices.queryUserId(_client, _redis, user);

        if (!getUserId.success)
            return NotFound(getUserId);

        // get channel editors
        var getEditors = await EmoteServices.getChannelEditors(_client, _redis, getUserId.result);

        if (!getEditors.success)
            return NotFound(getEditors);

        return Ok(getEditors);
    }

    [HttpGet("getusereditoraccess")]
    public async Task<ActionResult> GetUserEditorAccess(string user)
    {
        // get user id
        var getUserId = await EmoteServices.queryUserId(_client, _redis, user);

        if (!getUserId.success)
            return NotFound(getUserId);
        var getUserEditorAccess = await EmoteServices.getUserEditorAccess(
            _client,
            _redis,
            getUserId.result
        );

        if (!getUserEditorAccess.success)
        {
            return NotFound(getUserEditorAccess);
        }

        return Ok(getUserEditorAccess);
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddEmoteinEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string> { success = false };
        if (request.targetemotes.Count() == 0)
        {
            response.error = _errorlist.Where(x => x.errorCode == "7010").Single();
            return BadRequest(response);
        }

        if (request.targetemotes.Count() > 1 && !string.IsNullOrEmpty(request.emoterename))
        {
            response.error = _errorlist.Where(x => x.errorCode == "7011").Single();
            return BadRequest(response);
        }

        // see if the emotes provided already in objectId format
        EmoteServices.CheckObjectID(
            request.targetemotes,
            out List<Emotes> idlist,
            out List<Emotes> querylist
        );

        List<Emotes> emotes_toAdd = new();
        List<FailedEmotes> emotes_fuzzy = new();
        List<Emotes> emotes_failedSearch = new();

        // added id directly into the list since we need to use the id during add query
        emotes_toAdd.AddRange(idlist);

        // search the emotes
        if (request.source != null || request.owner != null)
        {
            List<Emotes> sourceEmotes = new();
            if (request.source != null)
            {
                var getUserSourceId = await EmoteServices.queryUserId(
                    _client,
                    _redis,
                    request.source
                );

                if (!getUserSourceId.success && getUserSourceId.result == null)
                    return NotFound(getUserSourceId);

                var getUserSourceEmotes = await EmoteServices.getChannelEmotes(
                    _client,
                    _redis,
                    getUserSourceId.result
                );

                if (!getUserSourceEmotes.success)
                    return NotFound(getUserSourceEmotes);

                sourceEmotes = _mapper.Map<List<Emotes>>(getUserSourceEmotes.result);
            }
            else if (request.owner != null)
            {
                var getOwnerId = await EmoteServices.queryUserId(_client, _redis, request.owner);

                if (!getOwnerId.success)
                {
                    getOwnerId.error.errorMessage = "Please provide a valid owner username";
                    return NotFound(getOwnerId);
                }

                var getOwnerEmotes = await EmoteServices.getOwnerEmotes(
                    _client,
                    _redis,
                    getOwnerId.result
                );

                if (!getOwnerEmotes.success)
                {
                    return NotFound(getOwnerEmotes);
                }

                sourceEmotes = _mapper.Map<List<Emotes>>(getOwnerEmotes.result);
            }

            var findEmotes = (
                from queries in querylist
                join emotesdict in sourceEmotes on queries.Name equals emotesdict.Name
                select new Emotes
                {
                    Name = emotesdict.Name,
                    Id = emotesdict.Id,
                    Rename = queries.Name
                }
            ).ToList();

            emotes_toAdd.AddRange(findEmotes);

            var emotes_notExactMatch = querylist
                .Select(x => x.Name)
                .Except(findEmotes.Select(x => x.Name))
                .Select(x => new Emotes { Name = x })
                .ToList();

            // search for the nearest match for not exact match emotes
            var emoteNames = sourceEmotes.Select(x => x.Name).ToList();
            var fuzzyEmotes = new List<FailedEmotes>();

            foreach (var emote in emotes_notExactMatch)
            {
                var fuzzyResult = Process.ExtractTop(emote.Name, emoteNames, cutoff: 70);

                if (!fuzzyResult.Any())
                {
                    continue;
                }

                fuzzyEmotes.Add(
                    new FailedEmotes
                    {
                        Name = emote.Name,
                        Fuzzy = fuzzyResult.First().Value,
                        Score = fuzzyResult.First().Score
                    }
                );
            }

            emotes_fuzzy.AddRange(fuzzyEmotes);

            // get the list of not found emotes
            var emotes_notFound = emotes_notExactMatch
                .Select(x => x.Name)
                .Except(fuzzyEmotes.Select(x => x.Name))
                .Select(x => new Emotes { Name = x })
                .ToList();

            emotes_failedSearch.AddRange(emotes_notFound);
        }
        else
        {
            foreach (var emote in querylist)
            {
                var searchEmote = await EmoteServices.searchEmote(_client, _redis, emote.Name);

                if (!searchEmote.success)
                {
                    emotes_failedSearch.Add(emote);
                    continue;
                }

                var emoteAM = _mapper.Map<Emotes>(searchEmote.result);
                emotes_toAdd.Add(emoteAM);
            }
        }

        // get target channel info
        var targetEmoteSetId = "";
        var targetUserId = "";
        List<Emotes> channelemotes = new();
        {
            var getUserId = await EmoteServices.queryUserId(_client, _redis, request.targetchannel);

            if (!getUserId.success)
                return NotFound(getUserId);

            targetUserId = getUserId.result;

            var getEmoteSetId = await EmoteServices.getUserActiveEmoteSetId(
                _client,
                _redis,
                targetUserId
            );

            if (!getEmoteSetId.success)
                return NotFound(getEmoteSetId);

            targetEmoteSetId = getEmoteSetId.result;
        }

        // add the emotes
        var addEmote_failed = new List<Emotes>();
        var addEmote_success = new List<Emotes>();

        if (emotes_toAdd.Count() > 0)
        {
            foreach (var emote in emotes_toAdd)
            {
                if (!string.IsNullOrEmpty(request.emoterename))
                    emote.Rename = request.emoterename;

                if (request.defaultname)
                    emote.Rename = "";

                var addresult = await EmoteServices.AddEmote(
                    _client,
                    _redis,
                    emote.Id!,
                    targetEmoteSetId!,
                    emote.Rename!
                );

                if (!addresult.success)
                {
                    emote.errorMessage = addresult.error.errorMessage;
                    addEmote_failed.Add(emote);
                    continue;
                }

                var resultlist = _mapper.Map<List<Emotes>>(addresult.result);

                var addedEmote = resultlist.Where(x => x.Id == emote.Id).SingleOrDefault();

                if (addedEmote == null)
                {
                    addEmote_success.Add(emote);
                    continue;
                }

                addedEmote.Rename = emote.Rename;
                addEmote_success.Add(addedEmote);
            }
        }

        // give info if some emotes are not found or failed to add
        string actionmessage = "";

        if (addEmote_success.Any())
            actionmessage +=
                $"| Successfully added this emote(s): {EmoteServices.EmoteStringBuilder(addEmote_success)} | ";

        if (addEmote_failed.Any())
            actionmessage += $" | {EmoteServices.EmoteErrorBuilder(addEmote_failed)} | ";

        if (emotes_fuzzy.Any())
        {
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", emotes_fuzzy.Select(x => x.ToString()).ToList())} | ";
        }

        if (emotes_failedSearch.Any())
            actionmessage +=
                $"| Failed to search emote(s): {EmoteServices.EmoteStringBuilder(emotes_failedSearch)} | ";

        response.success = true;
        response.result = actionmessage;

        return Ok(response);
    }

    [HttpPost("remove")]
    public async Task<ActionResult> RemoveEmoteFromEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string>() { success = false };

        if (request.targetemotes.Count() == 0)
        {
            response.error = _errorlist.Where(x => x.errorCode == "7010").Single();
            return BadRequest(response);
        }

        EmoteServices.CheckObjectID(
            request.targetemotes,
            out List<Emotes> idlist,
            out List<Emotes> querylist
        );

        // get targetchannel user id
        var getUserId = await EmoteServices.queryUserId(_client, _redis, request.targetchannel);

        if (!getUserId.success)
            return NotFound(getUserId);

        var emotelist = await EmoteServices.getChannelEmotes(_client, _redis, getUserId.result);

        if (!emotelist.success)
            return NotFound(emotelist);

        var sourceEmotes = _mapper.Map<List<Emotes>>(emotelist.result);

        List<Emotes> emotes_toRemove = new();
        List<Emotes> emotes_notExist = new();
        List<FailedEmotes> emotes_fuzzy = new();

        emotes_toRemove.AddRange(idlist);

        // see if the target emotes exist in emoteset
        if (idlist.Count() != 0)
        {
            var findEmotes = (
                from emotes in sourceEmotes
                join ids in idlist on emotes.Id equals ids.ToString()
                select emotes
            ).ToList();

            emotes_toRemove.AddRange(findEmotes);

            var emotesNotFound = idlist
                .Select(x => x.Id)
                .Except(findEmotes.Select(x => x.Id))
                .Select(x => new Emotes { Id = x })
                .ToList();

            emotes_notExist.AddRange(emotesNotFound);
        }

        if (querylist.Count() != 0)
        {
            var findEmotes = (
                from emotes in sourceEmotes
                join queries in querylist on emotes.Name equals queries.Name
                select emotes
            ).ToList();

            emotes_toRemove.AddRange(findEmotes);

            var emotes_NotExactMatch = querylist
                .Select(x => x.Name)
                .Except(findEmotes.Select(x => x.Name))
                .Select(x => new Emotes { Name = x })
                .ToList();

            // search for the nearest match for not exact match emotes
            var emoteNames = sourceEmotes.Select(x => x.Name).ToList();

            foreach (var emote in emotes_NotExactMatch)
            {
                var fuzzyResult = Process.ExtractTop(emote.Name, emoteNames, cutoff: 70);

                if (!fuzzyResult.Any())
                    continue;

                emotes_fuzzy.Add(
                    new FailedEmotes
                    {
                        Name = emote.Name,
                        Fuzzy = fuzzyResult.First().Value,
                        Score = fuzzyResult.First().Score
                    }
                );
            }

            // get the list of not found emote
            var notFoundEmote = emotes_NotExactMatch
                .Select(x => x.Name)
                .Except(emotes_fuzzy.Select(x => x.Name))
                .Select(x => new Emotes { Name = x })
                .ToList();

            emotes_notExist.AddRange(notFoundEmote);
        }

        // get activeemotesetid
        var getEmoteSetId = await EmoteServices.getUserActiveEmoteSetId(
            _client,
            _redis,
            getUserId.result
        );

        if (!getEmoteSetId.success)
            return NotFound(getEmoteSetId);

        // remove emotes
        var emote_removeFailed = new List<Emotes>();
        var emote_removeSuccess = new List<Emotes>();
        if (emotes_toRemove.Count > 0)
        {
            foreach (var emote in emotes_toRemove)
            {
                var removeresult = await EmoteServices.RemoveEmote(
                    _client,
                    _redis,
                    emote.Id,
                    getEmoteSetId.result
                );

                if (!removeresult.success)
                    emote_removeFailed.Add(emote);
                else
                    emote_removeSuccess.Add(emote);
            }
        }

        // give info if some emotes are not found or failed to add
        string actionmessage = "";

        if (emote_removeSuccess.Any())
            actionmessage +=
                $"| Successfully removed the emote(s): {EmoteServices.EmoteStringBuilder(emote_removeSuccess)} | ";

        if (emote_removeFailed.Any())
            actionmessage += $" | {EmoteServices.EmoteErrorBuilder(emote_removeFailed)} | ";

        if (emotes_fuzzy.Any())
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", emotes_fuzzy.Select(x => x.ToString()).ToList())} | ";

        if (emotes_notExist.Any())
            actionmessage +=
                $"| Emote(s) not exist in the channel: {EmoteServices.EmoteStringBuilder(emotes_notExist)} | ";

        response.success = true;
        response.result = actionmessage;

        return Ok(response);
    }

    [HttpPost("rename")]
    public async Task<ActionResult> RenameEmoteFromEmoteSet(ModifyEmoteinEmoteSetRequest request)
    {
        var response = new ApiResponse<string>() { success = false };

        EmoteServices.CheckObjectID(
            request.targetemotes,
            out List<Emotes> idlist,
            out List<Emotes> querylist
        );

        // will only take 1 emote rename for now
        var targetemote = request.targetemotes[0];

        // if (string.IsNullOrEmpty(request.emoterename))
        // {
        //     response.error = _errorlist.Where(x => x.errorCode == "7014").Single();
        //     return BadRequest(response);
        // }

        // get the channel's emote
        var getUserId = await EmoteServices.queryUserId(_client, _redis, request.targetchannel);

        if (!getUserId.success && getUserId.result == null)
            return NotFound(getUserId);

        var getUserSourceEmotes = await EmoteServices.getChannelEmotes(
            _client,
            _redis,
            getUserId.result
        );

        if (!getUserSourceEmotes.success)
            return NotFound(getUserSourceEmotes);

        var sourceEmotes = _mapper.Map<List<Emotes>>(getUserSourceEmotes.result);

        var renameEmote_failed = new List<Emotes>();
        var renameEmote_success = new List<Emotes>();
        var renameEmote_notFound = new List<Emotes>();
        var renameEmote_fuzzy = new List<FailedEmotes>();

        try
        {
            // search the emote
            Emotes? searchEmote = null;

            if (idlist.Count != 0 && querylist.Count() == 0)
            {
                searchEmote = sourceEmotes.Where(x => x.Id == idlist[0].Id).SingleOrDefault();
            }
            else
            {
                searchEmote = sourceEmotes
                    .Where(x => x.Name == querylist[0].Name)
                    .SingleOrDefault();
            }

            if (searchEmote == null)
            {
                if (querylist.Any())
                {
                    var emoteNames = sourceEmotes.Select(x => x.Name).ToList();
                    var fuzzyResult = Process.ExtractTop(querylist[0].Name, emoteNames, cutoff: 70);

                    if (!fuzzyResult.Any())
                    {
                        renameEmote_notFound.Add(new Emotes() { Name = request.targetemotes[0] });
                        throw new Exception();
                    }

                    renameEmote_fuzzy.Add(
                        new FailedEmotes
                        {
                            Name = querylist[0].Name,
                            Fuzzy = fuzzyResult.First().Value,
                            Score = fuzzyResult.First().Score
                        }
                    );
                }

                if (idlist.Any())
                    renameEmote_notFound.Add(new Emotes() { Name = request.targetemotes[0] });

                throw new Exception();
            }

            // get the channel emote set id
            var getEmoteSetId = await EmoteServices.getUserActiveEmoteSetId(
                _client,
                _redis,
                getUserId.result
            );

            if (!getEmoteSetId.success)
                return NotFound(getEmoteSetId);

            // remove the emote
            var removeresult = await EmoteServices.RemoveEmote(
                _client,
                _redis,
                searchEmote!.Id!,
                getEmoteSetId.result!
            );

            if (!removeresult.success)
            {
                renameEmote_failed.Add(
                    new Emotes()
                    {
                        Name = request.targetemotes[0],
                        Rename = request.emoterename,
                        errorMessage = removeresult.error.errorMessage
                    }
                );

                throw new Exception(removeresult.error.errorMessage);
            }

            // re-add the emote with rename
            var readdresult = await EmoteServices.AddEmote(
                _client,
                _redis,
                searchEmote.Id!,
                getEmoteSetId.result!,
                request.emoterename
            );

            if (!readdresult.success)
            {
                renameEmote_failed.Add(
                    new Emotes()
                    {
                        Name = request.targetemotes[0],
                        Rename = request.emoterename,
                        errorMessage = removeresult.error.errorMessage
                    }
                );

                throw new Exception(removeresult.error.errorMessage);
            }

            renameEmote_success.Add(
                new Emotes() { Name = request.targetemotes[0], Rename = request.emoterename }
            );
        }
        catch (Exception ex) { }
        finally
        {
            if (renameEmote_success.Any())
                response.result +=
                    $"| Succcessfully rename {request.targetemotes[0]} to {renameEmote_success[0].RenameString()} | ";

            if (renameEmote_failed.Any())
                response.result += $" | {EmoteServices.EmoteErrorBuilder(renameEmote_failed)} | ";

            if (renameEmote_fuzzy.Any())
                response.result +=
                    $" | Not found the emote(s). Did you mean : {string.Join(" ; ", renameEmote_fuzzy.Select(x => x.ToString()).ToList())} | ";

            if (renameEmote_notFound.Any())
                response.result +=
                    $"| Failed to search emotes: {EmoteServices.EmoteStringBuilder(renameEmote_notFound)} | ";
        }

        response.success = true;
        return Ok(response);
    }
}
