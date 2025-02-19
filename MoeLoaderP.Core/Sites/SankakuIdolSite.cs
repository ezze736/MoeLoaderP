﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     idol.sankakucomplex.com
/// </summary>
public class SankakuIdolSite : MoeSite
{
    private readonly string[] _pass =
    {
        "girlis2018", "moel006", "moel107", "moel482", "moel367", "moel876", "moel652", "moel740", "moel453", "moel263",
        "moel395"
    };

    private readonly string[] _user =
    {
        "girltmp", "mload006", "mload107", "mload482", "mload367", "mload876", "mload652", "mload740", "mload453",
        "mload263", "mload395"
    };

    private string _idolQuery;

    public SankakuIdolSite()
    {
        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        Config = new MoeSiteConfig
        {
            IsSupportKeyword = true,
            IsSupportRating = true,
            IsSupportResolution = true,
            IsSupportScore = true
        };
    }

    public override string HomeUrl => "https://idol.sankakucomplex.com";
    public override string DisplayName => "SankakuComplex[Idol]";
    public override string ShortName => "sankakucomplex-idol";

    public async Task LoginAsync(CancellationToken token)
    {
        Net = new NetOperator(Settings, this);
        const string loginhost = "https://iapi.sankakucomplex.com";
        var accountIndex = new Random().Next(0, _user.Length);
        var tempuser = _user[accountIndex];
        var temppass = GetSankakuPwHash(_pass[accountIndex]);
        var tempappkey = GetSankakuAppkey(tempuser);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"login", tempuser},
            {"password_hash", temppass},
            {"appkey", tempappkey}
        });
        Net.HttpClientHandler.UseCookies = true;
        var client = Net.Client;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SCChannelApp/2.3 (Android; idol)");
        client.DefaultRequestHeaders.Referrer = new Uri(HomeUrl);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var respose = await client.PostAsync(new Uri($"{loginhost}/user/authenticate.json"), content, token);
        if (respose.IsSuccessStatusCode)
            IsUserLogin = true;
        else
            Ex.ShowMessage("idol登陆失败", null, Ex.MessagePos.Window);
        _idolQuery = $"{loginhost}/post/index.json?login={tempuser}&password_hash={temppass}&appkey={tempappkey}&";
    }

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        if (!IsUserLogin) await LoginAsync(token);
        if (!IsUserLogin) return new SearchedPage();
        var query = $"{_idolQuery}page={para.PageIndex}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
        var list = await Net.GetJsonAsync(query, token: token);
        if (list == null) return new SearchedPage {Message = "获取Json失败"};
        var imgs = new SearchedPage();
        const string https = "https:";
        foreach (var item in list)
        {
            var img = new MoeItem(this, para);
            img.Width = $"{item.width}".ToInt();
            img.Height = $"{item.height}".ToInt();
            img.Id = $"{item.id}".ToInt();
            img.Score = $"{item.fav_count}".ToInt();
            img.Uploader = $"{item.uploader_name}";
            img.DetailUrl = $"{HomeUrl}/post/show/{img.Id}";
            img.Date = $"{item.created_at?.s}".ToDateTime();
            foreach (var tag in Ex.GetList(item.tags)) img.Tags.Add($"{tag.name}");
            img.IsExplicit = $"{item.rating}" == "e";
            img.Urls.Add(DownloadTypeEnum.Thumbnail, $"{https}{item.preview_url}", img.DetailUrl);
            img.Urls.Add(DownloadTypeEnum.Medium, $"{https}{item.sample_url}", img.DetailUrl);
            img.Urls.Add(DownloadTypeEnum.Origin, $"{https}{item.file_url}", img.DetailUrl);
            img.OriginString = $"{item}";
            imgs.Add(img);
        }

        return imgs;
    }


    /// <summary>
    ///     计算用于登录等账号操作的AppKey
    /// </summary>
    /// <param name="user">用户名</param>
    /// <returns></returns>
    private static string GetSankakuAppkey(string user)
    {
        return Sha1($"sankakuapp_{user.ToLower()}_Z5NE9YASej", Encoding.Default).ToLower();
    }

    /// <summary>
    ///     计算密码sha1
    /// </summary>
    /// <param name="password">密码</param>
    /// <returns></returns>
    private static string GetSankakuPwHash(string password)
    {
        return Sha1($"choujin-steiner--{password}--", Encoding.Default).ToLower();
    }

    /// <summary>
    ///     SHA1加密
    /// </summary>
    /// <param name="content">字符串</param>
    /// <param name="encode">编码</param>
    /// <returns></returns>
    private static string Sha1(string content, Encoding encode)
    {
        try
        {
            var sha1 = SHA1.Create();
            var bytesIn = encode.GetBytes(content);
            var bytesOut = sha1.ComputeHash(bytesIn);
            var result = BitConverter.ToString(bytesOut);
            result = result.Replace("-", "");
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("SHA1Error:" + ex.Message);
        }
    }
}