using System;
using System.Net;

namespace StaticPagesDownloader.Main
{
    /// <summary>
    /// 
    /// </summary>
    internal static class RetryableAction
    {
        /// <summary>
        /// 実行処理
        /// </summary>
        public static bool Exec(Func<bool> function)
        {
            // 例外が起きても3回ほどリトライ
            for (int i = 2; i >= 0; i--)
            {
                // 成功した場合は抜ける
                if (function()) { return true; }
            }

            return false;
        }
    }

    /// <summary>
    /// ダウンロードアクション
    /// </summary>
    internal class DownloadAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        public static bool Exec(Func<bool> function)
        {
            try
            {
                if (function()) { return true; }
            }
            // Web例外 かつ 404の場合はそのまま例外処理を行う。
            catch (WebException ex)
                when (ex.Status == WebExceptionStatus.ProtocolError &&
                        ex.Response is HttpWebResponse &&
                        ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
            {
                throw;
            }
            // 例外発生時は何もしない
            catch { }
            return false;
        }
    }
}
