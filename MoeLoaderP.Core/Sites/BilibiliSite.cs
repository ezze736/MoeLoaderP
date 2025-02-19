﻿using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     B站画友、摄影 Fixed 20200315
/// </summary>
public class BilibiliSite : MoeSite
{
    public BilibiliSite()
    {
        Config.IsSupportKeyword = true;
        Config.IsSupportScore = true;

        Lv2Cat = new Categories(Config);
        Lv2Cat.Adds("画友", "摄影(COS)", "摄影(私服)");
        Lv2Cat.EachSubAdds("最新", "最热");

        DownloadTypes.Add("原图", DownloadTypeEnum.Origin);
        LoginPageUrl = "https://passport.bilibili.com/login";
    }

    public override string HomeUrl => "https://h.bilibili.com";

    public override string DisplayName => "哔哩哔哩";

    public override string ShortName => "bilibili";

    public override bool VerifyCookieAndSave(CookieCollection ccol)
    {
        return ccol.Any(cookie => cookie.Name.Equals("DedeUserID", StringComparison.OrdinalIgnoreCase));
    }

    //public override async Task<bool> ThumbAsync(MoeItem item, CancellationToken token)
    //{
    //    if (!IsLogin()) return false;
    //    var r = await AccountNet.Client.PostAsync()
    //}

    public override async Task<SearchedPage> GetRealPageAsync(SearchPara para, CancellationToken token)
    {
        var page = new SearchedPage();
        if (para.Keyword.IsEmpty())
            await SearchByNewOrHot(para, page, token);
        else
            await SearchByKeyword(para, page, token);
        return page;
    }

    public async Task SearchByNewOrHot(SearchPara para, MoeItems imgs, CancellationToken token)
    {
        const string api = "https://api.vc.bilibili.com/link_draw/v2";
        var type = para.Lv3MenuIndex == 0 ? "new" : "hot";
        var count = para.CountLimit > 20 ? 20 : para.CountLimit;
        var api2 = "";
        switch (para.Lv2MenuIndex)
        {
            case 0:
                api2 = $"{api}/Doc/list";
                break;
            case 1:
            case 2:
                api2 = $"{api}/Photo/list";
                break;
        }

        var net = new NetOperator(Settings, this);
        var json = await net.GetJsonAsync(api2, new Pairs
        {
            {"category", para.Lv2MenuIndex == 0 ? "all" : para.Lv2MenuIndex == 1 ? "cos" : "sifu"},
            {"type", type},
            {"page_num", $"{para.PageIndex - 1}"},
            {"page_size", $"{count}"}
        }, token: token);


        foreach (var item in Ex.GetList(json?.data?.items))
        {
            var cat = para.Lv2MenuIndex == 0 ? "/d" : "/p";
            var img = new MoeItem(this, para)
            {
                Uploader = $"{item.user?.name}",
                Id = $"{item.item?.doc_id}".ToInt()
            };
            img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
            var i0 = item.item?.pictures[0];
            img.Width = $"{i0?.img_width}".ToInt();
            img.Height = $"{i0?.img_height}".ToInt();
            img.Date = $"{item.item?.upload_time}".ToDateTime();
            img.Urls.Add(1, $"{i0?.img_src}@336w_336h_1e_1c.jpg", HomeUrl + cat);
            img.Urls.Add(2, $"{i0?.img_src}@1024w_768h.jpg");
            img.Urls.Add(4, $"{i0?.img_src}");
            img.Title = $"{item.item?.title}";
            var list = item.item?.pictures as JArray;
            if (list?.Count > 1)
                foreach (var pic in item.item.pictures)
                {
                    var child = new MoeItem(this, para);
                    child.Urls.Add(1, $"{pic.img_src}@336w_336h_1e_1c.jpg", HomeUrl + cat);
                    child.Urls.Add(2, $"{pic.img_src}@1024w_768h.jpg", HomeUrl + cat);
                    child.Urls.Add(4, $"{pic.img_src}");
                    child.Width = $"{pic.img_width}".ToInt();
                    child.Height = $"{pic.img_height}".ToInt();
                    img.ChildrenItems.Add(child);
                }

            img.GetDetailTaskFunc = async t => await GetSearchByNewOrHotDetailTask(img, t);
            img.OriginString = $"{item}";
            imgs.Add(img);
        }

        var c = $"{json?.data.total_count}".ToInt();
        Ex.ShowMessage($"共搜索到{c}张，已加载至{para.PageIndex}页，共{c / para.CountLimit}页", null, Ex.MessagePos.InfoBar);
    }

    public async Task SearchByKeyword(SearchPara para, MoeItems imgs, CancellationToken token)
    {
        const string api = "https://api.bilibili.com/x/web-interface/search/type";
        var newOrHotOrder = para.Lv3MenuIndex == 0 ? "pubdate" : "stow";
        var drawOrPhotoCatId = para.Lv2MenuIndex == 0 ? "1" : "2";
        var pairs = new Pairs
        {
            {"search_type", "photo"},
            {"page", $"{para.PageIndex}"},
            {"order", newOrHotOrder},
            {"keyword", para.Keyword.ToEncodedUrl()},
            {"category_id", drawOrPhotoCatId}
        };
        var net = new NetOperator(Settings, this);
        var json = await net.GetJsonAsync(api, pairs, token: token);
        if (json == null) return;
        foreach (var item in Ex.GetList(json.data?.result))
        {
            var img = new MoeItem(this, para);
            img.Urls.Add(1, $"{item.cover}@336w_336h_1e_1c.jpg");
            img.Urls.Add(2, $"{item.cover}@1024w_768h.jpg");
            img.Urls.Add(4, $"{item.cover}");
            img.Id = $"{item.id}".ToInt();
            img.Score = $"{item.like}".ToInt();
            img.Rank = $"{item.rank_offset}".ToInt();
            img.Title = $"{item.title}";
            img.Uploader = $"{item.uname}";
            img.GetDetailTaskFunc = async cancellationToken =>
                await GetSearchByKeywordDetailTask(img, para, cancellationToken);
            img.DetailUrl = $"https://h.bilibili.com/{img.Id}";
            img.OriginString = $"{item}";
            imgs.Add(img);
        }

        var c = $"{json.data?.numResults}".ToInt();
        Ex.ShowMessage($"共搜索到{c}张，已加载至{para.PageIndex}页，共{c / para.CountLimit}页", null, Ex.MessagePos.InfoBar);
    }

    public async Task GetSearchByKeywordDetailTask(MoeItem img, SearchPara para, CancellationToken token)
    {
        var query = $"https://api.vc.bilibili.com/link_draw/v1/doc/detail?doc_id={img.Id}";
        var json = await new NetOperator(Settings, this).GetJsonAsync(query, token: token);
        var item = json.data?.item;
        if (item == null) return;
        if ((item.pictures as JArray)?.Count > 1)
        {
            var i = 0;
            foreach (var pic in Ex.GetList(item.pictures))
            {
                var child = new MoeItem(this, para);
                child.Urls.Add(1, $"{pic.img_src}@336w_336h_1e_1c.jpg");
                child.Urls.Add(2, $"{pic.img_src}@1024w_768h.jpg");
                child.Urls.Add(4, $"{pic.img_src}");
                if (i == 0)
                {
                    img.Width = $"{pic.img_width}".ToInt();
                    img.Height = $"{pic.img_height}".ToInt();
                }

                img.ChildrenItems.Add(child);
                i++;
            }
        }
        else if ((item.pictures as JArray)?.Count == 1)
        {
            var pic = json.data?.item?.pictures[0];
            img.Width = $"{pic?.img_width}".ToInt();
            img.Height = $"{pic?.img_height}".ToInt();
            img.Urls.Add(4, $"{pic?.img_src}");
        }

        foreach (var tag in Ex.GetList(item.tags)) img.Tags.Add($"{tag.name}");

        img.Date = $"{json.data?.item?.upload_time}".ToDateTime();
        if (img.Date == null) img.DateString = $"{item.upload_time}";
    }

    public async Task GetSearchByNewOrHotDetailTask(MoeItem img, CancellationToken token)
    {
        var query = $"https://api.vc.bilibili.com/link_draw/v1/doc/detail?doc_id={img.Id}";
        var json = await new NetOperator(Settings, this).GetJsonAsync(query, showSearchMessage: false, token: token);
        var item = json.data?.item;
        if (item == null) return;
        foreach (var tag in Ex.GetList(item.tags)) img.Tags.Add($"{tag.name}");

        img.Score = $"{item.vote_count}".ToInt();
    }
}