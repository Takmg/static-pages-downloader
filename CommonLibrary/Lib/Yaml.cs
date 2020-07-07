using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace CommonLibrary.Lib
{
    public class Yaml
    {
        /// <summary>
        /// YAMLファイル名
        /// </summary>
        public string SettingFileName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string RootNodeName { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Yaml(string settingFilename, string rootNodeName)
        {
            SettingFileName = settingFilename;
            RootNodeName = rootNodeName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        private YamlNode Read(string key)
        {
            try
            {
                using (var input = new StreamReader(SettingFileName, Encoding.UTF8))
                {
                    // YAMLをロード
                    var yaml = new YamlStream();
                    yaml.Load(input);

                    // 
                    var mapping = (YamlMappingNode)yaml.Documents[0].RootNode[RootNodeName];
                    return mapping.Children[new YamlScalarNode(key)];
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 設定ファイルを取得
        /// </summary>
        public string ReadString(string key)
        {
            return (Read(key) as YamlScalarNode)?.Value ?? string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string[] ReadArray(string key)
        {
            try
            {
                // アイテムを取得する
                var items = (Read(key) as YamlSequenceNode) ?? new YamlSequenceNode();

                // 配列を返す
                return items.Children.Select(e => e.ToString()).ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool? ReadBool(string key)
        {
            try
            {
                return bool.Parse(ReadString(key));
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int? ReadInt(string key)
        {
            try
            {
                return int.Parse(ReadString(key));
            }
            catch { }
            return null;
        }
    }
}
