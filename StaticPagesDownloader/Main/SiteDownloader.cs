using CommonLibrary.Lib;
using CSScriptLib;
using ExCSS;
using HtmlAgilityPack;
using JSBeautifyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
            { "a" , "href" },{ "img" , "src" },{ "script" , "src" },{ "link" , "href" },
        };

        /// <summary>
        /// 
        /// </summary>
        private dynamic HookFunc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private dynamic ScriptFunc { get; set; }

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
            HookFunc = CSScript
                .RoslynEvaluator
                .LoadMethod(File.ReadAllText("./Script/CNodeHook.csx"));
            _logger.WriteLine("CNodeHook.csxの読み込み完了");
            ScriptFunc = CSScript
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
            }

            // 時間計測停止
            st.Stop();
            _logger.WriteLine($"HTMLダウンロード数   => {_htmlDepth.Count}");
            _logger.WriteLine($"非HTMLダウンロード数 => {_isDownloadedResources.Count}");
            _logger.WriteLine($"かかった時間 => {st.Elapsed}");
            _logger.WriteSeparator();
            _logger.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        private Uri DownloadRecursive(Uri targetUri, int depth, int maxDepth)
        {
            // ダウンロードメソッド用内部関数
            Func<Uri, string> callbackUri = (sourceUri) =>
            {
                // ソースパスを作成する
                var sourcePath = new SPath(_settings, targetUri, false);

                // ダウンロードを行う
                var savePathUri = DownloadRecursive(sourceUri, depth - 1, maxDepth);

                // HTTP通信だった場合、そのまま返す
                if (!savePathUri.IsFile) { return savePathUri.ToString(); }

                // HTML書き換え
                return sourcePath.MakeRelativeString(savePathUri);
            };


            try
            {
                // HTMLDocumentを取得する
                var htmlDoc = FetchHtmlDocument(targetUri);
                if (htmlDoc == null) { return targetUri; }

                // HTMLノードを取得する
                var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//html");

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
                    _logger.WriteLine($"【保存完了】 階層 => {depth} , 使用メモリ => {_process.WorkingSet64 / 1024 / 1024}MB");
                    _logger.WriteLine($" {path.SiteUri}");
                    _logger.WriteLine($" {path.ExportPathString}");
                    _logger.WriteLine();

                    // 再帰処理を行う
                    return path.ExportPathUri;
                }
                // HTMLだった場合
                else
                {
                    // 無視するURIだった場合は何もしない
                    if (IsIgnoreUri(targetUri)) { return targetUri; }

                    // パスの作成を行う
                    var path = new SPath(_settings, targetUri, false);
                    path.ReplaceToHtmlPath();

                    // 深さが探索以上の深さになってしまった場合や既に浅いところで解析済みだった場合は
                    // 出力パスを返す
                    if (depth <= maxDepth || !UpdateHtmlDepth(targetUri, depth)) { return path.ExportPathUri; }

                    // ファイル保存済みだった場合、簡易的な解析を行う
                    var analyzed = false;
                    lock (_fileLockObj) { analyzed = File.Exists(path.ExportPathString); }

                    // 探索ノードを全て取得する(解析済みの場合はAタグのみを解析する)
                    IEnumerable<(HtmlNode, string)> nodes = new List<(HtmlNode, string)>();
                    foreach (var tags in _analyzeTags.Where(e => analyzed ? e.Key == "a" : true))
                    {
                        var node = htmlDoc.DocumentNode.SelectNodes($"//{tags.Key}");
                        var nodeVal = node?.Select(e => (e, e.GetAttributeValue(tags.Value, string.Empty)));
                        if (node != null) { nodes = nodes.Concat(nodeVal); }
                    }

                    // Aタグの内容を全て深さを-2にして解析済みにする。(深さの奥深くで処理させない為。)
                    foreach (var node in nodes.Where(e => e.Item1.Name == "a" && !string.IsNullOrEmpty(e.Item2)))
                    {
                        var downloadUri = new Uri(targetUri, node.Item2);
                        UpdateHtmlDepth(downloadUri, depth - 2);
                    }

                    // 全ノードを巡回する
                    var options = new ParallelOptions() { MaxDegreeOfParallelism = _settings.UseThread && (depth - maxDepth == 1) ? -1 : 1 };
                    Parallel.ForEach(nodes, options, (node) =>
                    {
                        // リンク先の取得/存在しない場合は何もしない
                        var src = node.Item2;
                        if (string.IsNullOrEmpty(src)) { return; }

                        // 各ノードの処理
                        if (!HookFunc.Main(node)) { return; }

                        // DownloadURLの作成
                        var downloadUri = new Uri(targetUri, src);

                        // 再帰的に処理を行う
                        var relativePath = callbackUri(downloadUri);
                        node.Item1.SetAttributeValue(_analyzeTags[node.Item1.Name], relativePath);
                    });

                    // 解析済みなら以降の処理を省く
                    if (analyzed) { return path.ExportPathUri; }

                    // HTMLのJavaScriptの書き換え
                    var jsNodes = htmlDoc.DocumentNode.SelectNodes($"//script");
                    foreach (var js in jsNodes?.ToArray() ?? new HtmlNode[0])
                    {
                        if (string.IsNullOrEmpty(js.InnerHtml.Trim())) { continue; }
                        js.InnerHtml = ConvertJsText(js.InnerHtml, path, callbackUri);
                    }

                    // HTMLのスタイルタグ書き換え
                    var styleNodes = htmlDoc.DocumentNode.SelectNodes($"//style");
                    foreach (var style in styleNodes?.ToArray() ?? new HtmlNode[0])
                    {
                        if (string.IsNullOrEmpty(style.InnerHtml.Trim())) { continue; }
                        style.InnerHtml = ConvertStyleText(style.InnerHtml, path, callbackUri);
                    }

                    // HTMLの保存
                    lock (_fileLockObj)
                    {
                        Directory.CreateDirectory(path.ExportDir);
                        htmlDoc.Save(path.ExportPathString, _settings.HtmlEncoding);
                    }

                    // ログ出力
                    _logger.WriteLine($"【保存完了】 階層 => {depth} , 使用メモリ => {_process.WorkingSet64 / 1024 / 1024}MB");
                    _logger.WriteLine($" {path.SiteUri}");
                    _logger.WriteLine($" {path.ExportPathString}");
                    _logger.WriteLine();

                    return path.ExportPathUri;
                }
            }
            catch (Exception e)
            {
                // ログ出力
                _logger.WriteSeparator();
                _logger.WriteLine($"【例外処理】 : {e.Message}");
                _logger.WriteLine($"             : {targetUri.OriginalString}");
                _logger.WriteLine($"{e.StackTrace}");
                _logger.WriteSeparator();
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
            jsText = ScriptFunc.ConvertJsUrl(jsText, path.SiteUri, callback);
            return jsText;
        }

        /// <summary>
        /// CSSファイルを変換する
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        private string ConvertStyleText(string styleText, SPath path, Func<Uri, string> callback)
        {
            // Stylesheetの読み込み
            Stylesheet sheet;
            var byteArray = Encoding.UTF8.GetBytes(styleText);
            using (var stream = new MemoryStream(byteArray)) { sheet = new StylesheetParser().Parse(stream); }

            // CSSのフォーマット
            using (var tw = new StringWriter())
            {
                sheet.ToCss(tw, new ReadableStyleFormatter());
                tw.Flush();
                styleText = tw.ToString();
            }

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
            var func = new Func<bool>(() => { doc = htmlWeb.Load(targetUri.AbsoluteUri); return true; });
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
        private bool IsIgnoreUri(Uri uri)
        {
            // 保存先対象のリソースが保存ダウンロードURI配下でない場合はダウンロード続行
            // ・ホストが同一か、パスが配下かの確認を行っている。
            if (_settings.DownloadUri.Host != uri.Host) { return true; }
            if (!uri.LocalPath.StartsWith(_settings.DownloadUri.LocalPath)) { return true; }

            // ダウンロードURIからの相対パスを取得
            var relativePath = uri.PathAndQuery.Substring(_settings.DownloadUri.LocalPath.Length);
            if (!relativePath.StartsWith("/")) { relativePath = "/" + relativePath; }

            // 相対パス内に無視するURIが含まれている場合は無視する
            if (_settings.IgnoreList.Any(e => relativePath.Contains(e))) { return true; }

            // ここまでくる場合は何もしない
            return false;
        }
    }
}
