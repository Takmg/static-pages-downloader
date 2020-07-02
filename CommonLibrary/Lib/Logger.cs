using System;
using System.IO;
using System.Text;

namespace CommonLibrary.Lib
{
    /// <summary>
    /// ログ出力用クラス
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// 現在のタイムスタンプ
        /// </summary>
        public static readonly string Timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        /// <summary>
        /// 出力先ファイル名
        /// </summary>
        private string FileName { get; set; }

        /// <summary>
        /// コンソール出力モード
        /// </summary>
        private bool ConsoleOutputFlag { get; set; }

        /// <summary>
        /// 出力パス
        /// </summary>
        public string ExportPath { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filename"></param>
        public Logger(string filename = null, bool consoleOutputFlag = true)
        {
            // ファイル名が存在しない場合はファイル名を作成する。
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"{Timestamp}-log.log";
            }

            // ファイル名の指定
            FileName = filename;

            // コンソール出力フラグ
            ConsoleOutputFlag = consoleOutputFlag;
        }

        /// <summary>
        /// 書き込みを行う
        /// </summary>
        /// <param name="sa"></param>
        public void WriteLine(string value)
        {
            // コンソールログ出力を実施
            if (ConsoleOutputFlag) { Console.WriteLine(value); }

            // 文字コードを指定
            Encoding enc = Encoding.GetEncoding("UTF-8");

            // StreamWriterを実行
            string loggerPath = $"{ExportPath}/log";
            Directory.CreateDirectory(loggerPath);
            using (StreamWriter writer = new StreamWriter($"{loggerPath}/{FileName}", true, enc))
            {
                writer.WriteLine(value);
            }
        }

        /// <summary>
        /// セパレータの描画
        /// </summary>
        public void WriteSeparator()
        {
            WriteLine("---------------------------------------------------------------------");
        }
    }
}
