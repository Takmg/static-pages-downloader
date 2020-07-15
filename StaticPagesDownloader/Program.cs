using CommonLibrary.Lib;
using StaticPagesDownloader.Main;
using System;
using System.IO;
using System.Text;

namespace StaticPagesDownloader
{
    class Program
    {

        static void Main(string[] args)
        {
            // ロガーの作成
            var logger = new Logger();
            logger.ExportPath = Directory.GetCurrentDirectory();

            try
            {
                // ダウンロードの実行
                var pd = new SiteDownloader(logger, CreateSettings());
                pd.Download();
            }
            catch (Exception e)
            {
                logger.WriteLine($"【例外処理】 : {e.Message}");
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static GlobalSettings CreateSettings()
        {
            // YAMLインスタンスの作成
            var yaml = new Yaml("settings.yml", "common");

            // 設定ファイルの読み込み
            var depth = yaml.ReadInt("search_depth") ?? 5;
            var dlUri = yaml.ReadString("download_uri");
            var ignoreList = yaml.ReadArray("ignore_list");
            var encoding = yaml.ReadString("encoding");
            var usethread = yaml.ReadBool("multithread") ?? false;
            var baseTagRelative = yaml.ReadBool("basetag_relative") ?? false;
            var savePath = yaml.ReadString("save_path");
            if (savePath.StartsWith("./"))
            {
                savePath = Directory.GetCurrentDirectory() + "/" + savePath;
            }
            if (!savePath.EndsWith("/")) { savePath += "/"; }

            // GlobalSettingsの作成
            var gsettings = new GlobalSettings();
            gsettings.DownloadUri = new Uri(dlUri);
            gsettings.SaveRootDir = new Uri(savePath);
            gsettings.SearchDepth = depth;
            gsettings.HtmlEncoding = Encoding.GetEncoding(encoding);
            gsettings.IgnoreList = ignoreList;
            gsettings.UseThread = usethread;
            gsettings.BaseTagRelative = baseTagRelative;

            return gsettings;
        }
    }
}
