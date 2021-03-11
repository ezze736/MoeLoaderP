﻿using System.Threading;
using System.Threading.Tasks;

// Booru 类型通用站点集合
namespace MoeLoaderP.Core.Sites
{
    /// <summary>
    /// yande.re fixed 2021.1.1
    /// </summary>
    public class YandeSite : BooruSite
    {
        public override string HomeUrl => "https://yande.re";
        public override string DisplayName => "Yande";
        public override string ShortName => "yande";

        public override string GetHintQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"limit","15" },
                {"order","count" },
                {"name",$"{para.Keyword.ToEncodedUrl()}" }
            };
            return $"{HomeUrl}/tag.xml{pairs.ToPairsString()}";
        }

        public override string GetPageQuery(SearchPara para)
        {
            var pairs = new Pairs
            {
                {"page", $"{para.PageIndex}"},
                {"limit", $"{para.Count}"},
                {"tags", para.Keyword.ToEncodedUrl()}
            };
            return $"{HomeUrl}/post.xml{pairs.ToPairsString()}";
        }
    }

    /// <summary>
    /// behoimi.org fixed 2021.2.8
    /// </summary>
    public class BehoimiSite : BooruSite
    {
        public override string HomeUrl => "http://behoimi.org";
        public override string DisplayName => "Behoimi";
        public override string ShortName => "behoimi";
        public override string GetThumbnailReferer(MoeItem item) => "http://behoimi.org/post";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tag/index.xml?limit=8&order=count&name={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/post/index.xml?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
    }

    /// <summary>
    /// safebooru.org fixed 2021.2.8
    /// </summary>
    public class SafebooruSite : BooruSite
    {
        public override string HomeUrl => "https://safebooru.org";
        public override string DisplayName => "Safebooru";
        public override string ShortName => "safebooru";
        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=tag&q=index&order=name&limit=8&name={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override string UrlPre => "";

        public override async Task<MoeItems> GetRealPageImagesAsync(SearchPara para, CancellationToken token)
        {
            var r = await base.GetRealPageImagesAsync(para, token);

            foreach (var item in r)
            {
                item.Urls[0].Url = item.Urls[0].Url.Replace(".png", ".jpg").Replace(".jpeg", ".jpg");
            }

            return r;
        }

        public override string GetDetailPageUrl(MoeItem item) => $"{HomeUrl}/index.php?page=post&s=view&id={item.Id}";
    }

    /// <summary>
    /// danbooru.donmai.us fixed 2021.2.8
    /// </summary>
    public class DonmaiSite : BooruSite
    {
        public override string HomeUrl => "https://danbooru.donmai.us";
        public override string DisplayName => "Donmai";
        public override string ShortName => "donmai";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tags/autocomplete.json?search%5Bname_matches%5D={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override SiteTypeEnum SiteType => SiteTypeEnum.Json;

        public override string GetDetailPageUrl(MoeItem item)
            => $"{HomeUrl}/posts/{item.Id}";
    }

    /// <summary>
    /// lolibooru.moe fixed 2021.2.8
    /// </summary>
    public class LolibooruSite : BooruSite
    {
        public override string HomeUrl => "https://lolibooru.moe";
        public override string DisplayName => "Lolibooru";
        public override string ShortName => "lolibooru";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tag.xml?limit=8&order=count&name={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/post.xml?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

    }

    /// <summary>
    /// atfbooru.ninja fixed 2021.2.8
    /// </summary>
    public class AtfbooruSite : BooruSite
    {
        public override string HomeUrl => "https://booru.allthefallen.moe/";
        public override string DisplayName => "Atfbooru";
        public override string ShortName => "atfbooru";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/tags/autocomplete.json?search%5Bname_matches%5D={para.Keyword.ToEncodedUrl()}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/posts.json?page={para.PageIndex}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";

        public override SiteTypeEnum SiteType => SiteTypeEnum.Json;
    }

    /// <summary>
    /// rule34.xxx fixed 2021/2/8
    /// </summary>
    public class Rule34Site : BooruSite
    {
        public override string HomeUrl => "https://rule34.xxx";
        public override string DisplayName => "Rule34";
        public override string ShortName => "rule34";

        public override string GetHintQuery(SearchPara para)
            => $"{HomeUrl}/autocomplete.php?q={para.Keyword}";

        public override string GetPageQuery(SearchPara para)
            => $"{HomeUrl}/index.php?page=dapi&s=post&q=index&pid={para.PageIndex - 1}&limit={para.Count}&tags={para.Keyword.ToEncodedUrl()}";
    }

    
}
