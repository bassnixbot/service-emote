using EmoteService.Models;
using FuzzySharp;
using Microsoft.AspNetCore.Mvc;
using UtilsLib;

namespace EmoteService.Services;

public static class EmoteServices
{
    public static async Task<ApiResponse<string>> EmotePreview(
        PreviewRequest request,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<string> { success = false };

        if (request.targetemotes.Count() == 0)
        {
            response.error = UtilsLib
                .UtilsClient.GetErrorList.Where(x => x.errorCode == "EmoteService-7010")
                .Single();
            return response;
        }

        SevenTVServices.CheckObjectID(
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
                var getEmote = await SevenTVServices.getEmote(
                    dependency.client,
                    emoteid.Id.ToString()
                );

                if (!getEmote.success)
                {
                    emoteid.errorMessage = getEmote!.error!.errorMessage;
                    preview_failed.Add(emoteid);
                    continue;
                }

                var emoteDetails = dependency.mapper.Map<Emotes>(getEmote.result);

                preview_success.Add(emoteDetails);
            }
        }

        var preview_fuzzy = new List<FailedEmotes>();

        if (request.source != null && querylist.Count() != 0)
        {
            var getUserSourceId = await SevenTVServices.queryUserId(
                dependency.client,
                request.source
            );

            if (!getUserSourceId.success && getUserSourceId.result == null)
            {
                return getUserSourceId;
            }

            var getUserSourceEmotes = await SevenTVServices.getChannelEmotes(
                dependency.client,
                getUserSourceId.result
            );

            if (!getUserSourceEmotes.success)
            {
                response.error = getUserSourceId.error;
                return response;
            }

            var sourceEmotes = dependency.mapper.Map<List<Emotes>>(getUserSourceEmotes.result);

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
            actionmessage += $" | {SevenTVServices.EmoteErrorBuilder(preview_failed)} | ";

        if (preview_fuzzy.Any())
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", preview_fuzzy.Select(x => x.ToString()).ToList())} | ";

        response.success = true;
        response.result = actionmessage;

        return response;
    }

    [HttpGet("searchemotes")]
    public static async Task<ApiResponse<List<string>>> SearchEmotes(
        string channel,
        string? query,
        string? tags,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<List<string>>() { success = false };
        var getUserId = await SevenTVServices.queryUserId(dependency.client, channel);

        if (getUserId.success == false)
        {
            response.error = getUserId.error;
            return response;
        }

        var emotelist = await SevenTVServices.getChannelEmotes(dependency.client, getUserId.result);

        if (!emotelist.success)
        {
            response.error = emotelist.error;
            return response;
        }

        List<string> emotes = new();
        if (!string.IsNullOrEmpty(tags))
        {
            emotes = emotelist
                .result.Where(x => x.Data.Tags.Any(tag => tag.ToLower().Contains(tags.ToLower())))
                .Select(x => x.Name)
                .ToList();
            return new ApiResponse<List<string>> { success = true, result = emotes };
        }

        if (!string.IsNullOrEmpty(query))
        {
            emotes = emotelist
                .result.Where(x => x.Name.ToLower().Contains(query.ToLower() ?? ""))
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToList();
            return new ApiResponse<List<string>> { success = true, result = emotes };
        }

        // default fallback
        emotes = emotelist.result.Select(x => x.Name).ToList();
        return new ApiResponse<List<string>> { success = true, result = emotes };
    }

    public static async Task<ApiResponse<List<string>>> GetChannelEditors(
        string user,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<List<string>>() { success = false };
        // get user id
        var getUserId = await SevenTVServices.queryUserId(dependency.client, user);

        if (!getUserId.success)
        {
            response.error = getUserId.error;
            return response;
        }

        // get channel editors
        var getEditors = await SevenTVServices.getChannelEditors(
            dependency.client,
            getUserId.result
        );
        return getEditors;
    }

    public static async Task<ApiResponse<List<string>>> GetUserEditorAccess(
        string user,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<List<string>>() { success = false };

        // get user id
        var getUserId = await SevenTVServices.queryUserId(dependency.client, user);

        if (!getUserId.success)
        {
            response.error = getUserId.error;
            return response;
        }
        
        var getUserEditorAccess = await SevenTVServices.getUserEditorAccess(
            dependency.client,
            getUserId.result
        );

        if (!getUserEditorAccess.success)
        {
            return getUserEditorAccess;
        }

        return getUserEditorAccess;
    }

    [HttpPost("add")]
    public static async Task<ApiResponse<string>> AddEmoteinEmoteSet(
        ModifyEmoteinEmoteSetRequest request,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<string> { success = false };
        if (request.targetemotes.Count() == 0)
        {
            response.error = UtilsLib
                .UtilsClient.GetErrorList.Where(x => x.errorCode == "7010")
                .Single();
            return response;
        }

        if (request.targetemotes.Count() > 1 && !string.IsNullOrEmpty(request.emoterename))
        {
            response.error = UtilsLib
                .UtilsClient.GetErrorList.Where(x => x.errorCode == "EmoteService-7011")
                .Single();
            return response;
        }

        // see if the emotes provided already in objectId format
        SevenTVServices.CheckObjectID(
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
                var getUserSourceId = await SevenTVServices.queryUserId(dependency.client, request.source);

                if (!getUserSourceId.success && getUserSourceId.result == null)
                    return getUserSourceId;

                var getUserSourceEmotes = await SevenTVServices.getChannelEmotes(
                    dependency.client,
                    getUserSourceId.result
                );

                if (!getUserSourceEmotes.success) {
                    response.error = getUserSourceEmotes.error;
                    return response;
                }

                sourceEmotes = dependency.mapper.Map<List<Emotes>>(getUserSourceEmotes.result);
            }
            else if (request.owner != null)
            {
                var getOwnerId = await SevenTVServices.queryUserId(dependency.client, request.owner);

                if (!getOwnerId.success)
                {
                    getOwnerId.error.errorMessage = "Please provide a valid owner username";
                    return getOwnerId;
                }

                var getOwnerEmotes = await SevenTVServices.getOwnerEmotes(
                    dependency.client,
                    getOwnerId.result
                );

                if (!getOwnerEmotes.success)
                {
                    response.error = getOwnerEmotes.error;
                    return response;
                }

                sourceEmotes = dependency.mapper.Map<List<Emotes>>(getOwnerEmotes.result);
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
                var searchEmote = await SevenTVServices.searchEmote(dependency.client, emote.Name);

                if (!searchEmote.success)
                {
                    emotes_failedSearch.Add(emote);
                    continue;
                }

                var emoteAM = dependency.mapper.Map<Emotes>(searchEmote.result);
                emotes_toAdd.Add(emoteAM);
            }
        }

        // get target channel info
        var targetEmoteSetId = "";
        var targetUserId = "";
        List<Emotes> channelemotes = new();
        {
            var getUserId = await SevenTVServices.queryUserId(dependency.client, request.clientinfo.channel);

            if (!getUserId.success)
                return getUserId;

            targetUserId = getUserId.result;

            var getEmoteSetId = await SevenTVServices.getUserActiveEmoteSetId(
                dependency.client,
                targetUserId
            );

            if (!getEmoteSetId.success)
                return getEmoteSetId;

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

                var addresult = await SevenTVServices.AddEmote(
                    dependency.client,
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

                var resultlist = dependency.mapper.Map<List<Emotes>>(addresult.result);

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
                $"| Successfully added this emote(s): {SevenTVServices.EmoteStringBuilder(addEmote_success)} | ";

        if (addEmote_failed.Any())
            actionmessage += $" | {SevenTVServices.EmoteErrorBuilder(addEmote_failed)} | ";

        if (emotes_fuzzy.Any())
        {
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", emotes_fuzzy.Select(x => x.ToString()).ToList())} | ";
        }

        if (emotes_failedSearch.Any())
            actionmessage +=
                $"| Failed to search emote(s): {SevenTVServices.EmoteStringBuilder(emotes_failedSearch)} | ";

        response.success = true;
        response.result = actionmessage;

        return response;
    }

    public static async Task<ApiResponse<string>> RemoveEmoteFromEmoteSet(
        ModifyEmoteinEmoteSetRequest request,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<string>() { success = false };

        if (request.targetemotes.Count() == 0)
        {
            response.error = UtilsLib
                .UtilsClient.GetErrorList.Where(x => x.errorCode == "EmoteService-7010")
                .Single();
            return response;
        }

        SevenTVServices.CheckObjectID(
            request.targetemotes,
            out List<Emotes> idlist,
            out List<Emotes> querylist
        );

        // get targetchannel user id
        var getUserId = await SevenTVServices.queryUserId(dependency.client, request.clientinfo.channel);

        if (!getUserId.success)
            return getUserId;

        var emotelist = await SevenTVServices.getChannelEmotes(dependency.client, getUserId.result);

        if (!emotelist.success) {
            response.error = emotelist.error;
            return response;
        }

        var sourceEmotes = dependency.mapper.Map<List<Emotes>>(emotelist.result);

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
        var getEmoteSetId = await SevenTVServices.getUserActiveEmoteSetId(
            dependency.client,
            getUserId.result
        );

        if (!getEmoteSetId.success)
            return getEmoteSetId;

        // remove emotes
        var emote_removeFailed = new List<Emotes>();
        var emote_removeSuccess = new List<Emotes>();
        if (emotes_toRemove.Count > 0)
        {
            foreach (var emote in emotes_toRemove)
            {
                var removeresult = await SevenTVServices.RemoveEmote(
                    dependency.client,
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
                $"| Successfully removed the emote(s): {SevenTVServices.EmoteStringBuilder(emote_removeSuccess)} | ";

        if (emote_removeFailed.Any())
            actionmessage += $" | {SevenTVServices.EmoteErrorBuilder(emote_removeFailed)} | ";

        if (emotes_fuzzy.Any())
            actionmessage +=
                $" | Not found the emote(s). Did you mean : {string.Join(" ; ", emotes_fuzzy.Select(x => x.ToString()).ToList())} | ";

        if (emotes_notExist.Any())
            actionmessage +=
                $"| Emote(s) not exist in the channel: {SevenTVServices.EmoteStringBuilder(emotes_notExist)} | ";

        response.success = true;
        response.result = actionmessage;

        return response;
    }

    [HttpPost("rename")]
    public static async Task<ApiResponse<string>> RenameEmoteFromEmoteSet(
        ModifyEmoteinEmoteSetRequest request,
        ServiceDependency dependency
    )
    {
        var response = new ApiResponse<string>() { success = false };

        SevenTVServices.CheckObjectID(
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
        var getUserId = await SevenTVServices.queryUserId(dependency.client, request.clientinfo.channel);

        if (!getUserId.success && getUserId.result == null)
            return getUserId;

        var getUserSourceEmotes = await SevenTVServices.getChannelEmotes(dependency.client, getUserId.result);

        if (!getUserSourceEmotes.success) {
            response.error = getUserSourceEmotes.error;
            return response;
        }

        var sourceEmotes = dependency.mapper.Map<List<Emotes>>(getUserSourceEmotes.result);

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
            var getEmoteSetId = await SevenTVServices.getUserActiveEmoteSetId(
                dependency.client,
                getUserId.result
            );

            if (!getEmoteSetId.success)
                return getEmoteSetId;

            // remove the emote
            var removeresult = await SevenTVServices.RemoveEmote(
                dependency.client,
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
            var readdresult = await SevenTVServices.AddEmote(
                dependency.client,
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
                response.result += $" | {SevenTVServices.EmoteErrorBuilder(renameEmote_failed)} | ";

            if (renameEmote_fuzzy.Any())
                response.result +=
                    $" | Not found the emote(s). Did you mean : {string.Join(" ; ", renameEmote_fuzzy.Select(x => x.ToString()).ToList())} | ";

            if (renameEmote_notFound.Any())
                response.result +=
                    $"| Failed to search emotes: {SevenTVServices.EmoteStringBuilder(renameEmote_notFound)} | ";
        }

        response.success = true;
        return response;
    }

    public static async Task<ApiResponse<bool>> CheckPerms(
        ClientInfo clientinfo,
        ServiceDependency dependency
    ) {
        var response = new ApiResponse<bool>() {success = false};
        var usereditoraccessres = await GetUserEditorAccess("bassnixbot", dependency);
        var channel = clientinfo.channel.ToLower();
        var username = clientinfo.userInfo.userName.ToLower();

        if (!usereditoraccessres.success) {
            response.error = new Error() {errorMessage = "There was an error while running the command", errorCode = "perm-error"};
            return response;
        }
        
        if (!usereditoraccessres.result!.Contains(channel) && channel != "bassnixbot") {
            response.error = new Error() {errorMessage = "Gulp bot dont have the 7tv editor access in this channel", errorCode = "perm-error"};
            return response;
        }

        var channeleditoraccessres = await GetChannelEditors(channel, dependency);

        if (!channeleditoraccessres.success) {
            response.error = new Error() {errorMessage = "There was an error while running the command", errorCode = "perm-error"};
            return response;
        }

        if (!channeleditoraccessres.result!.Contains(username) && username != channel) {
            response.error = new Error() {errorMessage = "bassni2Pout only 7tv editor can modify the emotes", errorCode = "perm-error"};
            return response;
        }

        response.success = true;
        response.result = true;
        return response;
    }
}
