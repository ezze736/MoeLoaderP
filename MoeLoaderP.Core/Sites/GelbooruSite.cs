﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoeLoaderP.Core.Sites;

/// <summary>
///     fixed 2021.5.13
/// </summary>
public class GelbooruSite : BooruSite
{
    public override string HomeUrl => "https://gelbooru.com";
    public override string DisplayName => "Gelbooru";
    public override string ShortName => "gelbooru";
    public override SiteTypeEnum SiteType => SiteTypeEnum.Xml2;

    public override Func<MoeItem, SearchPara, CancellationToken, Task> GetDetailTaskFunc { get; set; } = GetDetailTask;

    public override string GetDetailPageUrl(MoeItem item)
    {
        return $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";
    }

    public override string GetHintQuery(SearchPara para)
    {
        return $"{HomeUrl}/index.php?page=autocomplete2&term={para.Keyword}&type=tag_query&limit=10";
    }

    public override string GetPageQuery(SearchPara para)
    {
        return
            $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.CountLimit}&tags={para.Keyword.ToEncodedUrl()}";
    }

    public override async Task<AutoHintItems> GetAutoHintItemsAsync(SearchPara para, CancellationToken token)
    {
        var ahis = new AutoHintItems();
        var jsonlist = await new NetOperator(Settings, this).GetJsonAsync(GetHintQuery(para), token: token);
        foreach (var item in Ex.GetList(jsonlist))
            ahis.Add(new AutoHintItem
            {
                Word = $"{item.value}",
                Count = $"{item.post_count}"
            });
        return ahis;
    }

    public static async Task GetDetailTask(MoeItem img, SearchPara para, CancellationToken token)
    {
        var url = img.DetailUrl;
        var net = new NetOperator(img.Site.Settings, img.Site);
        var html = await net.GetHtmlAsync(url, null, false, token);
        if (html == null) return;
        var nodes = html.DocumentNode;
        img.Artist = nodes.SelectSingleNode("*//li[@class='tag-type-artist']/a[2]")?.InnerText.Trim();
        img.Character = nodes.SelectSingleNode("*//li[@class='tag-type-character']/a[2]")?.InnerText.Trim();
        img.Copyright = nodes.SelectSingleNode("*//li[@class='tag-type-copyright']/a[2]")?.InnerText.Trim();
    }
}