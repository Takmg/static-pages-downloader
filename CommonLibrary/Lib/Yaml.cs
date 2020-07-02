using System.IO;
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
        /// 設定ファイルを取得
        /// </summary>
        public string ReadString(string key)
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
                    return ((YamlScalarNode)mapping.Children[new YamlScalarNode(key)])?.Value;
                }
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
