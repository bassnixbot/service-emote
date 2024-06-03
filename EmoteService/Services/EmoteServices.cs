using EmoteService.GraphQl;
using EmoteService.Models;
using EmoteService.Utils;
using MongoDB.Bson;
using UtilsLib;

namespace EmoteService.Services;

public static class SevenTVServices
{
    public static async Task<ApiResponse<string>> queryUserId(
        ISevenTvClient client,
        string userquery
    )
    {
        var rediskey = $"7tv_id_{userquery}";
        var response = new ApiResponse<string> { success = false };

        try
        {
            var userid = await RedisLib.RedisClient.GetOrCacheObject<string>(
                rediskey,
                async (cacheparam) =>
                {
                    var result = await client.QueryUserId.ExecuteAsync(userquery);

                    if (result == null)
                        throw new Exception("7001");

                    if (result.Errors.Count() != 0 && result.Errors[0] != null)
                        throw new Exception(result.Errors[0].Message);

                    if (result.Data == null)
                        throw new Exception("7013");

                    if (result.Data.Users[0] == null)
                        throw new Exception("7002");

                    if (result.Data.Users[0].Id == ObjectId.Empty.ToString())
                        throw new Exception("7002");

                    cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                    cacheparam.value = result.Data.Users[0].Id;
                }
            );

            response.success = true;
            response.result = userid;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<
        ApiResponse<List<IGetFullUserDetails_User_Emote_sets_Emotes>?>
    > getChannelEmotes(ISevenTvClient client, string userid)
    {
        var response = new ApiResponse<List<IGetFullUserDetails_User_Emote_sets_Emotes>?>
        {
            success = false
        };

        var rediskey = $"channel_emotes_{userid}";
        List<IGetFullUserDetails_User_Emote_sets_Emotes>? channelEmotes = null;

        try
        {
            channelEmotes =
                await RedisLib.RedisClient.GetOrCacheObject<List<IGetFullUserDetails_User_Emote_sets_Emotes>?>(
                    rediskey,
                    async (cacheparam) =>
                    {
                        var getUserFullInfo = await client.GetFullUserDetails.ExecuteAsync(userid);

                        if (getUserFullInfo == null)
                            throw new Exception("7001");

                        if (
                            getUserFullInfo.Errors.Count() != 0
                            && getUserFullInfo.Errors[0] != null
                        )
                            throw new Exception(getUserFullInfo.Errors[0].Message);

                        if (getUserFullInfo.Data == null)
                            throw new Exception("7002");

                        // we get the active emote sets based on the twitch connection
                        // it is harcoded to the first item in the array
                        var userconnection = getUserFullInfo.Data.User.Connections;

                        if (
                            userconnection == null
                            || userconnection.Count() == 0
                            || userconnection[0] == null
                        )
                        {
                            throw new Exception("7003");
                        }

                        var emotelist = getUserFullInfo
                            .Data.User.Emote_sets.Where(x =>
                                x.Id == userconnection[0]!.Emote_set_id
                            )
                            .Select(x => x.Emotes)
                            .Single()
                            .ToList();

                        if (emotelist == null || emotelist.Count() == 0)
                        {
                            throw new Exception("7004");
                        }

                        cacheparam.expiry = (TimeSpan.FromMinutes(Config.redis.shortTimeout));
                        cacheparam.value = emotelist;
                    }
                );

            response.success = true;
            response.result = channelEmotes;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<
        ApiResponse<List<IGetFullUserDetails_User_Owned_emotes>?>
    > getOwnerEmotes(ISevenTvClient client, string userid)
    {
        var response = new ApiResponse<List<IGetFullUserDetails_User_Owned_emotes>?>
        {
            success = false
        };

        var rediskey = $"owner_emotes_{userid}";
        List<IGetFullUserDetails_User_Owned_emotes>? ownedEmotes = null;

        try
        {
            ownedEmotes =
                await RedisLib.RedisClient.GetOrCacheObject<List<IGetFullUserDetails_User_Owned_emotes>?>(
                    rediskey,
                    async (cacheparam) =>
                    {
                        var getUserFullInfo = await client.GetFullUserDetails.ExecuteAsync(userid);

                        if (getUserFullInfo == null)
                            throw new Exception("7001");

                        if (
                            getUserFullInfo.Errors.Count() != 0
                            && getUserFullInfo.Errors[0] != null
                        )
                            throw new Exception(getUserFullInfo.Errors[0].Message);

                        if (getUserFullInfo.Data == null)
                            throw new Exception("7002");

                        var emotelist = getUserFullInfo.Data.User.Owned_emotes.ToList();

                        if (emotelist == null || emotelist.Count == 0)
                            throw new Exception("7005");

                        cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                        cacheparam.value = emotelist;
                    }
                );

            response.success = true;
            response.result = ownedEmotes;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<List<string>>> getChannelEditors(
        ISevenTvClient client,
        string userid
    )
    {
        var response = new ApiResponse<List<string>> { success = false };
        var cachekey = $"channel_editors_{userid}";

        try
        {
            var getEditors = await RedisLib.RedisClient.GetOrCacheObject<List<string>>(
                cachekey,
                async (cacheparam) =>
                {
                    var userDetail = await client.GetFullUserDetails.ExecuteAsync(userid);

                    if (userDetail == null || userDetail.Data == null)
                        throw new Exception("7002");

                    var editors = userDetail.Data.User.Editors;

                    if (editors == null || editors.Count() == 0)
                        throw new Exception("7006");

                    cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                    cacheparam.value = editors.Select(x => x.User.Username).ToList();
                }
            );

            response.success = true;
            response.result = getEditors;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<List<string>>> getUserEditorAccess(
        ISevenTvClient client,
        string userid
    )
    {
        var response = new ApiResponse<List<string>> { success = false };
        var cachekey = $"editor_access_{userid}";

        try
        {
            var getEditorAccess = await RedisLib.RedisClient.GetOrCacheObject<List<string>>(
                cachekey,
                async (cacheparam) =>
                {
                    var userDetail = await client.GetFullUserDetails.ExecuteAsync(userid);

                    if (userDetail == null || userDetail.Data == null)
                        throw new Exception("7002");

                    var editors = userDetail.Data.User.Editor_of;

                    if (editors == null || editors.Count() == 0)
                        throw new Exception("7007");

                    cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                    cacheparam.value = editors.Select(x => x.User.Display_name.ToLower()).ToList();
                }
            );
            response.success = true;
            response.result = getEditorAccess;
            return response;
        }
        catch (Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<string>> getUserActiveEmoteSetId(
        ISevenTvClient client,
        string userid
    )
    {
        var response = new ApiResponse<string> { success = false };
        var cachekey = $"active_emote_setid_{userid}";

        try
        {
            var getEmoteSetId = await RedisLib.RedisClient.GetOrCacheObject<string>(
                cachekey,
                async (cacheparam) =>
                {
                    var userDetail = await client.GetFullUserDetails.ExecuteAsync(userid);

                    if (userDetail == null || userDetail.Data == null)
                        throw new Exception("7002");

                    // we get the active emote sets based on the twitch connection
                    // it is harcoded to the first item in the array
                    var userconnection = userDetail.Data.User.Connections;

                    if (
                        userconnection == null
                        || userconnection.Count() == 0
                        || userconnection[0] == null
                    )
                        throw new Exception("7003");

                    cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                    cacheparam.value = userconnection[0]!.Emote_set_id!;
                }
            );

            response.success = true;
            response.result = getEmoteSetId;
            return response;
        }
        catch (Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<IQueryEmotes_Emotes_Items>> searchEmote(
        ISevenTvClient client,
        string emotename
    )
    {
        var response = new ApiResponse<IQueryEmotes_Emotes_Items> { success = false };
        var cachekey = $"emote_search_{emotename}";

        try
        {
            var queryResult =
                await RedisLib.RedisClient.GetOrCacheObject<IQueryEmotes_Emotes_Items?>(
                    cachekey,
                    async (cacheparam) =>
                    {
                        cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                        var queryEmotes = await client.QueryEmotes.ExecuteAsync(emotename, 300);

                        if (queryEmotes == null)
                            throw new Exception("7001");

                        if (queryEmotes.Errors.Count != 0 && queryEmotes.Errors[0] != null)
                            throw new Exception(queryEmotes.Errors[0].Message);

                        if (queryEmotes.Data == null)
                        {
                            throw new Exception();
                        }

                        var orderedResult = queryEmotes
                            .Data.Emotes.Items.Where(x => x != null)
                            .OrderByDescending(x => x!.Channels.Total)
                            .ToList();

                        var exactmatch = orderedResult.FirstOrDefault(x => x!.Name == emotename);

                        if (exactmatch != null)
                            cacheparam.value = exactmatch;

                        var caseInsensitiveMatch = orderedResult.FirstOrDefault(x =>
                            x!.Name.ToLower() == emotename.ToLower()
                        );

                        if (caseInsensitiveMatch != null)
                            cacheparam.value = caseInsensitiveMatch;
                    }
                );

            if (queryResult == null)
            {
                response.error = UtilsLib.UtilsClient.GetErrorList.Where(x => x.errorCode == "7008").Single();
                return response;
            }

            response.success = true;
            response.result = queryResult;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<IGetEmote_EmotesByID>> getEmote(
        ISevenTvClient client,
        string emoteid
    )
    {
        var response = new ApiResponse<IGetEmote_EmotesByID> { success = false };
        var cachekey = $"emote_get_{emoteid}";

        try
        {
            var queryResult = await RedisLib.RedisClient.GetOrCacheObject<IGetEmote_EmotesByID>(
                cachekey,
                async (cacheparam) =>
                {
                    var req = new List<string> { emoteid };
                    var queryEmotes = await client.GetEmote.ExecuteAsync(req);

                    if (queryEmotes == null)
                        throw new Exception("7001");

                    if (queryEmotes.Errors.Count != 0 && queryEmotes.Errors[0] != null)
                        throw new Exception(queryEmotes.Errors[0].Message);

                    if (queryEmotes.Data == null)
                    {
                        throw new Exception();
                    }

                    var emoteData = queryEmotes
                        .Data.EmotesByID.Where(x => x.Id == emoteid)
                        .SingleOrDefault();

                    if (emoteData == null)
                    {
                        throw new Exception();
                    }

                    cacheparam.expiry = TimeSpan.FromMinutes(Config.redis.longTimeout);
                    cacheparam.value = emoteData;
                }
            );

            if (queryResult == null)
            {
                response.error = UtilsLib.UtilsClient.GetErrorList.Where(x => x.errorCode == "7008").Single();
                return response;
            }

            response.success = true;
            response.result = queryResult;
            return response;
        }
        catch (System.Exception ex)
        {
            response.error = HandleException(ex);
            return response;
        }
    }

    public static async Task<ApiResponse<List<IModifyEmote_EmoteSet_Emotes>>> AddEmote(
        ISevenTvClient client,
        string emoteid,
        string emotesetid,
        string emoteRename = ""
    )
    {
        var response = new ApiResponse<List<IModifyEmote_EmoteSet_Emotes>> { success = false };

        var addEmote = await client.ModifyEmote.ExecuteAsync(
            emotesetid,
            ListItemAction.Add,
            emoteid,
            emoteRename
        );

        if (addEmote == null || addEmote.Data == null)
        {
            response.error = UtilsLib.UtilsClient.GetErrorList.Where(x => x.errorCode == "7001").Single();
            return response;
        }

        if (addEmote.Errors.Count != 0 && addEmote.Errors[0] != null)
        {
            response.error = new Error
            {
                errorCode = addEmote.Errors[0].Code,
                errorMessage = addEmote.Errors[0].Message
            };

            return response;
        }

        if (addEmote.Data == null || addEmote.Data.EmoteSet == null)
        {
            response.error = new Error
            {
                errorMessage = "An unexpected error has been occured. Please try again later."
            };
            return response;
        }

        response.result = addEmote.Data.EmoteSet.Emotes.ToList();
        response.success = true;
        return response;
    }

    public static async Task<ApiResponse<List<IModifyEmote_EmoteSet_Emotes>>> RemoveEmote(
        ISevenTvClient client,
        string emoteid,
        string emotesetid
    )
    {
        var response = new ApiResponse<List<IModifyEmote_EmoteSet_Emotes>> { success = false };

        var removeEmote = await client.ModifyEmote.ExecuteAsync(
            emotesetid,
            ListItemAction.Remove,
            emoteid,
            ""
        );

        if (removeEmote == null || removeEmote.Data == null)
        {
            response.error = UtilsLib.UtilsClient.GetErrorList.Where(x => x.errorCode == "7001").Single();
            return response;
        }

        if (removeEmote.Errors.Count != 0 && removeEmote.Errors[0] != null)
        {
            response.error = new Error
            {
                errorCode = removeEmote.Errors[0].Code,
                errorMessage = removeEmote.Errors[0].Message
            };

            return response;
        }

        if (removeEmote.Data == null || removeEmote.Data.EmoteSet == null)
        {
            response.error = new Error
            {
                errorMessage = "An unexpected error has been occured. Please try again later."
            };
            return response;
        }

        response.result = removeEmote.Data.EmoteSet.Emotes.ToList();
        response.success = true;
        return response;
    }

    private static Error HandleException(System.Exception ex)
    {
        var errorlist = UtilsClient.GetErrorList;
        var errorDetail = errorlist
            .Where(x => x.errorCode == ex.Message)
            .SingleOrDefault();

        if (errorDetail != null)
            return errorDetail;

        // if (!string.IsNullOrEmpty(ex.Message))
        //     return new Error { errorMessage = ex.Message };

        return new Error
        {
            errorMessage = "An unexpected error has been occured. Please try again later.",
            errorStackTrace = ex.StackTrace
        };
    }

    // other app logic here
    public static void CheckObjectID(
        List<string> targetemotes,
        out List<Emotes> idlist,
        out List<Emotes> querylist
    )
    {
        idlist = new();
        querylist = new();

        foreach (var emote in targetemotes)
        {
            // check for if it's a uri
            Uri outUri;
            string possibleid;
            if (
                Uri.TryCreate(emote, UriKind.Absolute, out outUri)
                && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps)
            )
            {
                // Do something with your validated Absolute URI...
                possibleid = outUri.Segments.Last();
            }
            else
            {
                possibleid = emote;
            }

            // check if it's a ObjectId
            ObjectId objectId;

            if (ObjectId.TryParse(possibleid, out objectId))
            {
                // Valid ObjectId
                idlist.Add(new Emotes { Id = objectId.ToString() });
            }
            else
            {
                // else put it in querylist
                querylist.Add(new Emotes { Name = emote });
            }
        }
    }

    public static string EmoteStringBuilder(List<Emotes> emotelist)
    {
        List<string> outputlist = emotelist.Select(x => x.GetEmoteIdentifier()).ToList();
        return string.Join(" ", outputlist);
    }

    public static string EmoteErrorBuilder(List<Emotes> emotelist)
    {
        var outputList = emotelist
            .GroupBy(x => x.errorMessage)
            .Select(emotegroup =>
                $"{emotegroup.Key} ( {string.Join(" ", emotegroup.Select(x => x.GetEmoteIdentifier()))} )"
            )
            .ToList();

        return string.Join(" \\ ", outputList);
    }
}
