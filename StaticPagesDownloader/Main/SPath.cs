using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace StaticPagesDownloader.Main
{
    /// <summary>
    /// 
    /// </summary>
    internal class SPath
    {
        /// <summary>
        /// HTMLのURI
        /// </summary>
        public Uri SiteUri { get; private set; }

        /// <summary>
        /// 保存先URI
        /// </summary>
        public Uri ExportPathUri { get; private set; }

        /// <summary>
        /// パスの取得
        /// </summary>
        public string ExportPathString
        {
            get
            {
                return HttpUtility.UrlDecode(ExportPathUri.AbsolutePath);
            }
        }

        public string ExportDir
        {
            get
            {
                return Path.GetDirectoryName(ExportPathString);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Extension
        {
            get { return Path.GetExtension(ExportPathString); }
        }

        /// <summary>
        /// HtmlName
        /// </summary>
        public string DefaultHtmlName { get; set; } = "index.html";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SPath(GlobalSettings settings, Uri siteUri, bool queryRemoveFlg = true)
        {
            // サイトのURIを保存
            SiteUri = siteUri;

            // 属性文字列から取得したパスを正しいパスに書き換え
            var queryPath = HttpUtility.UrlDecode(queryRemoveFlg ? siteUri.LocalPath : siteUri.PathAndQuery);

            // queryパス内のディレクトリに使用できない文字を修正
            queryPath = escapeUri(queryPath);

            // パスにHTMLファイル名を設定
            if (queryPath.EndsWith("/")) { queryPath += DefaultHtmlName; }

            // カレントディレクトリに修正
            queryPath = "." + queryPath;

            // 他ドメインかどうかで処理を判断
            if (SiteUri.Host == settings.DownloadUri.Host)
            {
                ExportPathUri = new Uri(settings.SaveSiteDir, queryPath);
            }
            else
            {
                var targetUri = new Uri(settings.SaveGlobalDir, siteUri.Host + "/");
                ExportPathUri = new Uri(targetUri, queryPath);
            }
        }

        /// <summary>
        /// 自身からの相対パスを取得する
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Uri MakeRelativeUri(Uri uri)
        {
            var dir = Path.GetDirectoryName(ExportPathString);
            var dirUri = new Uri(dir + "/");
            return dirUri.MakeRelativeUri(uri);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string MakeRelativeString(Uri uri)
        {
            return HttpUtility.HtmlDecode(MakeRelativeUri(uri).OriginalString);
        }

        /// <summary>
        /// ReplaceToHtmlFile
        /// </summary>
        public void ReplaceToHtmlPath()
        {
            // HTMLの拡張子がついていない場合、HTML拡張子を最後尾につける
            if (ExportPathUri.AbsolutePath.EndsWith(".html")) { return; }
            ExportPathUri = new Uri(ExportPathUri.OriginalString + "____.html");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static string escapeUri(string arg)
        {
            var convertList = new Dictionary<char, char>{
                { '\\' , '￥'},
                { ':' , '：'},
                { '*' , '＊' },
                { '?' , '？' },
                { '"' , '”'},
                { '<' , '＜'},
                { '>' , '＞'},
                { '|' , '｜' },
                { '　' , ' '},   // 全角スペースはChromeで読めないので半角にする
            };

            return new string(arg.Select(e => convertList.Keys.Contains(e) ? convertList[e] : e).ToArray());
        }
    }
}
