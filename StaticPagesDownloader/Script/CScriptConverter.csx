using StaticPagesDownloader.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


static string ConverJsAjaxReplace(string jsText, Uri siteUri, Func<Uri, string> callback)
{
    // Textから該当箇所を読み込み
    var reg = new Regex(@"\$\.ajax\s*\(\s*\{\s*url\s*:\s*['""]{1}(?<url>.*?)['""]{1}.*?\}\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    var uriList = reg.Matches(jsText)?.Select(m => m.Groups["url"].Value);
    if (uriList == null || uriList.Count() <= 0) { return jsText; }

    // 全URLに対して処理を行う
    var replacedList = new List<string>();
    foreach (var uri in uriList)
    {
        var downloadUrl = new Uri(siteUri, uri);
        var replacedUri = callback(downloadUrl);
        replacedList.Add(replacedUri);
    }

    // Text部分を書き換え
    int cnt = 0;
    jsText = reg.Replace(jsText, (m) => {
        var reg2 = new Regex(@"url\s*:\s*['""]{1}.*?['""]{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return reg2.Replace(m.ToString(), $"url:'{replacedList[cnt++]}'");
    });

    // 戻す
    return jsText;
}


/// <summary>
/// 
/// </summary>
public string ConvertJsUrl(string jsText, Uri siteUri, Func<Uri, string> callback)
{
    // AjaxのURL文字列書き換え
    jsText = ConverJsAjaxReplace(jsText, siteUri, callback);
    
    // 
    
    return jsText;
}