using System;
using System.Text;

namespace StaticPagesDownloader.Main
{
    /// <summary>
    /// 以下はMainの初回で全て
    /// </summary>
    internal class GlobalSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public GlobalSettings() { }

        /// <summary>
        /// 保存先対象URI
        /// </summary>
        public Uri DownloadUri { get; set; }

        /// <summary>
        /// 保存先ディレクトリルート
        /// </summary>
        public Uri SaveRootDir { get; set; }

        /// <summary>
        /// 保存サイトディレクトリ
        /// </summary>
        public Uri SaveSiteDir { get { return new Uri(SaveRootDir, "./site/target/"); } }

        /// <summary>
        /// ドメインディレクトリ
        /// </summary>
        public Uri SaveGlobalDir { get { return new Uri(SaveRootDir, "./global/"); } }

        /// <summary>
        /// HTMLのエンコーディング
        /// </summary>
        public Encoding HtmlEncoding { get; set; }

        /// <summary>
        /// 無視リスト
        /// </summary>
        public string[] IgnoreList { get; set; }

        /// <summary>
        /// 検索の深さ
        /// </summary>
        public int SearchDepth { get; set; }

        /// <summary>
        /// スレッド使用フラグ
        /// </summary>
        public bool UseThread { get; set; }
    }
}
