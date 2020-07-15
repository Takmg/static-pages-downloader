using CommonLibrary.Lib;
using CSScriptLib;
using HtmlAgilityPack;
using JSBeautifyLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticPagesDownloader.Main
{
    class SiteDownloader
    {
        /// <summary>
        /// Logger(呼び出し元から渡される)
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// 設定ファイル
        /// </summary>
        private readonly GlobalSettings _settings;

        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object _fileLockObj = new object();

        /// <summary>
        /// 読み込みのロックオブジェクト
        /// </summary>
        private readonly object _webLockObj = new object();

        /// <summary>
        /// Htmlかどうか判断する為のDictionary
        /// </summary>
        private readonly List<Uri> _isDownloadedResources = new List<Uri>();

        /// <summary>
        /// パスの深さを検索
        /// </summary>
        private readonly Dictionary<Uri, int> _htmlDepth = new Dictionary<Uri, int>();

        /// <summary>
        /// タグ解析用
        /// </summary>
        private readonly Dictionary<string, string> _analyzeTags = new Dictionary<string, string>()
        {
            { "a" , "href" },{ "img" , "src" },{ "script" , "src" },{ "link" , "href" },{ "li","data-thumb"},
        };

        /// <summary>
        /// 
        /// </summary>
        private dynamic HtmlHookFunc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private dynamic NodeHookFunc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private dynamic ScriptHookFunc { get; set; }

        /// <summary>
        /// SavedFiles(Item1 => html , Item2 => Resources)
        /// </summary>
        private (int HtmlCount , int ResourceCount) SavedFiles ;

        /// <summary>
        /// プロセス情報
        /// </summary>
        private readonly Process _process = Process.GetCurrentProcess();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="ignoreList"></param>
        public SiteDownloader(Logger logger, GlobalSettings gsettings)
        {
            // メンバ変数の設定
            _logger = logger;
            _settings = gsettings;

            // CSXファイルのコンパイル処理
            _logger.WriteLine("★ダウンロード準備中...");
            _logger.WriteSeparator();
            HtmlHookFunc = CSScript
                .RoslynEvaluator
                .LoadMethod(File.ReadAllText("./Script/CHtmlHook.csx"));
            _logger.WriteLine("CHtmlHook.csxの読み込み完了");
            NodeHookFunc = CSScript
                .RoslynEvaluator
                .LoadMethod(File.ReadAllText("./Script/CNodeHook.csx"));
            _logger.WriteLine("CNodeHook.csxの読み込み完了");
            ScriptHookFunc = CSScript
                .RoslynEvaluator
                .LoadMethod(File.ReadAllText("./Script/CScriptConverter.csx"));
            _logger.WriteLine("CScriptConverter.csxの読み込み完了");
            _logger.WriteSeparator();
            _logger.WriteLine();
        }

        /// <summary>
        /// ダウンロードを実行する
        /// </summary>
        public void Download()
        {
            // 時間計測
            var st = new Stopwatch();
            st.Start();

            // ログ出力
            _logger.WriteLine("★HTMLのダウンロード開始");
            _logger.WriteSeparator();

            // 再帰的に処理を行う(トップ階層から順番にサイトを何度も巡回するが、この方が効率的)
            Uri res = null;
            for (int maxDepth = _settings.SearchDepth - 1; maxDepth >= 0; maxDepth--)
            {
                res = DownloadRecursive(_settings.DownloadUri, _settings.SearchDepth, maxDepth);

                // GC
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
                GC.WaitForFullGCComplete();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            // トップページの作成
            CreateTopPage(res);

            // 時間計測停止
            st.Stop();
            _logger.WriteLine($"HTMLダウンロード数   => {SavedFiles.HtmlCount}");
            _logger.WriteLine($"非HTMLダウンロード数 => {SavedFiles.ResourceCount}");
            _logger.WriteLine($"かかった時間 => {st.Elapsed}");
            _logger.WriteSeparator();
            _logger.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTopPage(Uri res)
        {
            // HTMLTemplate
            var relativePath = new Uri(_settings.SaveRootDir.ToString()).MakeRelativeUri(res);
            string HtmlTemplate = $@"
<!DOCTYPE html> 
<html>
    <head>
        <title>Jump</title>
        <meta http-equiv=""Refresh"" content=""0;URL={relativePath}"">
    </head>
    <body>
        <a href={relativePath}>jumpしない場合、ここをクリック</a>
    </body>
</html>";
            var saveFilePath = new Uri(_settings.SaveRootDir, "./index.html").OriginalString;
            File.WriteAllText(saveFilePath, HtmlTemplate);
        }

        /// <summary>
        /// 
        /// </summary>
        private Uri DownloadRecursive(Uri targetUri, int depth, int maxDepth)
        {
            try
            {
                // 対象URIを無視する
                if (IsIgnoreList(targetUri)) { return targetUri; }

                // HTMLDocumentを取得する
                var htmlDoc = FetchHtmlDocument(targetUri);
                if (htmlDoc == null) { return targetUri; }

                // HTMLノードを取得する
                var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//html");

                // baseUrlを取得する(BaseTagRelativeフラグがtrueの場合のみ)
                var baseUrl = !_settings.BaseTagRelative ? string.Empty :
                    htmlDoc.DocumentNode
                       .SelectNodes($"//base")
                       ?.FirstOrDefault()
                       ?.GetAttributeValue("href", string.Empty);

                // ダウンロードメソッド用内部関数
                Func<Uri, string> callbackUri = (sourceUri) =>
                {
                    // ダウンロードを行う
                    var savePathUri = DownloadRecursive(sourceUri, depth - 1, maxDepth);

                    // 戻り値がHTTP-URIだった場合、そのまま返す
                    if (!savePathUri.IsFile) { return savePathUri.ToString(); }

                    // もしbaseUriが存在している場合、baseUri基準で相対パスを取得する
                    if (!string.IsNullOrEmpty(baseUrl))
                    {
                        var basePath = new SPath(_settings, new Uri(_settings.DownloadUri, baseUrl), false);
                        return basePath.MakeRelativeString(savePathUri);
                    }

                    // HTML書き換え
                    var sourcePath = new SPath(_settings, targetUri, false);
                    return sourcePath.MakeRelativeString(savePathUri);
                };

                // 非HTMLだった場合
                if (htmlNodes == null || htmlNodes.Count <= 0)
                {
                    // パスの作成を行う
                    var path = new SPath(_settings, targetUri, true);

                    // 既にダウンロード済みか確認し、ダウンロード済みなら戻る
                    lock (_isDownloadedResources)
                    {
                        if (_isDownloadedResources.Contains(targetUri))
                        {
                            return path.ExportPathUri;
                        }
                        _isDownloadedResources.Add(targetUri);
                    }

                    // ファイルのダウンロードを行う
                    var func = new Func<bool>(() => DownloadFile(path));
                    var success = RetryableAction.Exec(() => DownloadAction.Exec(func));
                    if (!success) { return targetUri; }

                    // ファイルの書き換えを行う
                    ConvertFile(path, callbackUri);

                    // ログ出力
                    lock (_logger)
                    {
                        _logger.WriteLine($"【保存完了】 階層 => {depth} , 使用メモリ => {_process.WorkingSet64 / 1024 / 1024}MB");
                        _logger.WriteLine($" {path.SiteUri}");
                        _logger.WriteLine($" {path.ExportPathString}");
                        _logger.WriteLine();
                    }

                    // 保存数の加算
                    lock (_fileLockObj) { ++SavedFiles.ResourceCount; }

                    // 再帰処理を行う
                    return path.ExportPathUri;
                }
                // HTMLだった場合
                else
                {
                    // 無視するURIだった場合は何もしない
                    if (IsIgnoreHtml(targetUri)) { return targetUri; }

                    // パスの作成を行う
                    var path = new SPath(_settings, targetUri, false);
                    path.ReplaceToHtmlPath();

                    // 深さが探索以上の深さになってしまった場合や既に浅いところで解析済みだった場合は
                    // 出力パスを返す
                    if (depth <= maxDepth || !UpdateHtmlDepth(targetUri, depth)) { return path.ExportPathUri; }

                    // ファイル保存済みだった場合、簡易的な解析を行う
                    var analyzed = false;
                    lock (_fileLockObj) { analyzed = File.Exists(path.ExportPathString); }

                    // HTMLDocumentHook
                    if (!HtmlHookFunc.Main(targetUri, htmlDoc)) { return path.ExportPathUri; }

                    // 探索ノードを全て取得する(解析済みの場合はAタグのみを解析する)
                    var nodes =
                        _analyzeTags.Where(e => analyzed ? e.Key == "a" : true)
                                    .Select(e => (htmlDoc.DocumentNode.SelectNodes($"//{e.Key}"), e.Value))
                                    .Where(e => e.Item1 != null && e.Item1.Count > 0)
                                    .Select(e => e.Item1.Select(x => new { Node = x, Value = x.GetAttributeValue(e.Value, string.Empty) }))
                                    .SelectMany(e => e)
                                    .ToImmutableArray();

                    // Aタグの内容を全て深さを-2にして解析済みにする。(深さの奥深くで処理させない為。)
                    nodes.Where(e => e.Node.Name == "a" && e.Value.Length > 0)
                         .Select(e => new Uri(targetUri, e.Value))
                         .ToImmutableArray()
                         .ForEach(e => UpdateHtmlDepth(e, depth - 2));

                    // 全ノードを巡回する
                    var options = new ParallelOptions() { MaxDegreeOfParallelism = _settings.UseThread && (depth - maxDepth == 1) ? -1 : 1 };
                    Parallel.ForEach(nodes.Where(e => e.Value.Length > 0), options, (node) =>
                    {
                        // 各ノードの処理
                        if (!NodeHookFunc.Main(node.Node, node.Value)) { return; }

                        // DownloadURLの作成
                        var downloadUri = new Uri(targetUri, node.Value);

                        // 再帰的に処理を行う
                        var relativePath = callbackUri(downloadUri);
                        node.Node.SetAttributeValue(_analyzeTags[node.Node.Name], relativePath);
                    });

                    // 解析済みなら以降の処理を省く
                    if (analyzed) { return path.ExportPathUri; }

                    // HTMLのJavaScriptの書き換え
                    htmlDoc.DocumentNode.SelectNodes($"//script")
                           ?.Where(e => e != null)
                           .Where(e => !string.IsNullOrEmpty(e.InnerHtml.Trim()))
                           .ToImmutableArray()
                           .ForEach(e => e.InnerHtml = ConvertJsText(e.InnerHtml, path, callbackUri));

                    // HTMLのスタイルタグ書き換え
                    htmlDoc.DocumentNode.SelectNodes($"//style")
                           ?.Where(e => e != null)
                           .Where(e => !string.IsNullOrEmpty(e.InnerHtml.Trim()))
                           .ToImmutableArray()
                           .ForEach(e => e.InnerHtml = ConvertStyleText(e.InnerHtml, path, callbackUri));

                    // HTMLの保存
                    lock (_fileLockObj)
                    {
                        Directory.CreateDirectory(path.ExportDir);
                        htmlDoc.Save(path.ExportPathString, _settings.HtmlEncoding);
                    }

                    // 保存数の加算
                    lock (_fileLockObj) { ++SavedFiles.HtmlCount; }
                    
                    // ログ出力
                    lock (_logger)
                    {
                        _logger.WriteLine($"【保存完了】 階層 => {depth} , 使用メモリ => {_process.WorkingSet64 / 1024 / 1024}MB");
                        _logger.WriteLine($" {path.SiteUri}");
                        _logger.WriteLine($" {path.ExportPathString}");
                        _logger.WriteLine();
                    }
                    return path.ExportPathUri;
                }
            }
            catch (Exception e)
            {
                // ログ出力
                lock (_logger)
                {
                    _logger.WriteSeparator();
                    _logger.WriteLine($"【例外処理】 : {e.Message}");
                    _logger.WriteLine($"             : {targetUri.OriginalString}");
                    _logger.WriteLine($"{e.StackTrace}");
                    _logger.WriteSeparator();
                }
            }

            // 
            return targetUri;
        }


        /// <summary>
        /// ファイルのダウンロードを行う
        /// </summary>
        private bool DownloadFile(SPath path)
        {
            // ファイルのセーブを行う
            var saveFilePath = path.ExportPathString;

            lock (_fileLockObj)
            {
                // ファイルの保存前準備(ファイルが存在している場合はしない)
                if (File.Exists(saveFilePath)) { return true; }
                Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));

                // ダウンロード
                using (var client = new WebClient())
                {
                    client.DownloadFile(path.SiteUri, saveFilePath);
                }
            }

            return true;
        }


        /// <summary>
        /// ファイルのコンバートを行う
        /// </summary>
        private void ConvertFile(SPath path, Func<Uri, string> callback)
        {
            // 関数の取得
            Func<string, string> f = null;
            if (path.Extension == ".css") { f = (t) => ConvertStyleText(t, path, callback); }
            else if (path.Extension == ".js") { f = (t) => ConvertJsText(t, path, callback); }

            // 関数が存在しない場合は何もしない
            if (f == null) { return; }

            // それ以外の場合は処理を行う
            var text = File.ReadAllText(path.ExportPathString);
            text = f(text);
            File.WriteAllText(path.ExportPathString, text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="styleText"></param>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private string ConvertJsText(string jsText, SPath path, Func<Uri, string> callback)
        {
            jsText = new JSBeautify(jsText, new JSBeautifyOptions()).GetResult();
            jsText = $"\n{jsText}\n";
            jsText = ScriptHookFunc.ConvertJsUrl(jsText, path.SiteUri, callback);
            return jsText;
        }

        /// <summary>
        /// CSSファイルを変換する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private string ConvertStyleText(string styleText, SPath path, Func<Uri, string> callback)
        {
            // StyleのURL部分書き換え
            styleText = ConvertStyleUrl(styleText, path, callback);
            return styleText;
        }

        /// <summary>
        /// URL文字列を置き換える
        /// </summary>
        private static string ConvertStyleUrl(string styleText, SPath path, Func<Uri, string> callback)
        {
            // Textから該当箇所を読み込み
            var reg = new Regex("url\\('?\"?(?<url>.*?)\"?'?\\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var uriList = reg.Matches(styleText)?.Select(m => m.Groups["url"].Value);
            if (uriList == null || uriList.Count() <= 0) { return styleText; }

            // 全URLに対して処理を行う
            var replacedList = new List<string>();
            foreach (var uri in uriList)
            {
                var replacedUri = uri;

                // URL内のテキストがbase64の場合がある為、それ以外をダウンロード
                var reg2 = new Regex(".*?data:.*?base64.*?", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (reg2.Matches(uri).Count <= 0)
                {
                    // URL内のファイルをダウンロード
                    var downloadUrl = new Uri(path.SiteUri, replacedUri);
                    replacedUri = callback(downloadUrl);
                }
                replacedList.Add(replacedUri);
            }

            // Text部分を書き換え
            int cnt = 0;
            styleText = reg.Replace(styleText, (m) => { return $"url('{replacedList[cnt++]}')"; });

            // 戻す
            return styleText;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private HtmlDocument FetchHtmlDocument(Uri targetUri)
        {
            HtmlDocument doc = null;

            // HTMLを取得する
            var htmlWeb = new HtmlWeb();
            htmlWeb.OverrideEncoding = _settings.HtmlEncoding;
            var func = new Func<bool>(() =>
            {
                lock (_webLockObj) { doc = htmlWeb.Load(targetUri.AbsoluteUri); }
                return true;
            });
            var success = RetryableAction.Exec(() => DownloadAction.Exec(func));

            // HTML取得失敗した場合は何もしない
            if (!success || (int)htmlWeb.StatusCode >= 400)
            {
                _logger.WriteLine($"エラー : HTMLの取得に失敗しました。 {targetUri}");
                return null;
            }

            return doc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="depth"></param>
        private bool UpdateHtmlDepth(Uri uri, int depth)
        {
            lock (_htmlDepth)
            {
                if (_htmlDepth.ContainsKey(uri))
                {
                    // 履歴と比較して、現在の深度が浅い場合は更新する。
                    // それ以外は抜ける。
                    var historyDepth = _htmlDepth[uri];
                    if (historyDepth > depth) { return false; }
                }

                // 深度は以前より浅くなる。
                _htmlDepth[uri] = depth;
                return true;
            }
        }


        /// <summary>
        /// 無視するURIか確認
        /// </summary>
        /// <returns></returns>
        private bool IsIgnoreHtml(Uri uri)
        {
            // 保存先対象のリソースが保存ダウンロードURI配下でない場合はダウンロード続行
            // ・ホストが同一か、パスが配下かの確認を行っている。
            if (_settings.DownloadUri.Host != uri.Host) { return true; }
            if (!uri.LocalPath.StartsWith(_settings.DownloadUri.LocalPath)) { return true; }

            // ここまでくる場合は何もしない
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsIgnoreList(Uri uri)
        {
            // ignore
            var ignoreUri = _settings.IgnoreList.Where(e => e.StartsWith("//")).Select(e => new Uri(_settings.DownloadUri, e));
            var ignoreUri2 = _settings.IgnoreList.Where(e => e.StartsWith("/") && !e.StartsWith("//")).Select(e => new Uri(_settings.DownloadUri, e));
            var ignorePath = _settings.IgnoreList.Where(e => !e.StartsWith("/"));

            // 無視リスト内に現在のホストが存在する場合は無視
            if (ignoreUri.Any(e => e.Host == uri.Host) &&
                ignoreUri.Any(e => uri.AbsolutePath.StartsWith(e.AbsolutePath)))
            {
                return true;
            }

            // 無視リスト内に現在のパスが存在する場合は無視
            if (ignoreUri2.Any(e => uri.AbsolutePath.StartsWith(e.AbsolutePath))) { return true; }

            // DownloadUri配下に無視するパスがある場合はスキップ
            if (uri.Host == _settings.DownloadUri.Host &&
               uri.PathAndQuery.StartsWith(_settings.DownloadUri.AbsolutePath))
            {
                var p = uri.PathAndQuery.Substring(_settings.DownloadUri.AbsolutePath.Length);
                if (ignorePath.Any(e => p.Contains(e))) { return true; }
            }

            return false;
        }
    }
}
