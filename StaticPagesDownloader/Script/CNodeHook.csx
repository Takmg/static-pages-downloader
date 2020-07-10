using System;
using HtmlAgilityPack;

/// <summary>
/// LazyLoadの削除
/// </summary>
/// <param name="node"></param>
static void removeLazyLoad(HtmlNode node)
{
    // lazyload対応
    if (node.Name != "img") { return; }

    // lazy classを取得
    var clazz = node.Attributes["class"];
    if (clazz == null) { return; }

    // lazyを削除する
    clazz.Value = clazz.Value.Replace("lazy", string.Empty);

    // data-originalを削除する
    var original = node.GetAttributeValue("data-original", string.Empty);
    if (string.IsNullOrEmpty(original)) { return; }

    // src部分を書き換えて、data-originalを削除する
    node.SetAttributeValue("src", original);
    node.Attributes["data-original"].Remove();
}

/// <summary>
/// 
/// </summary>
/// <param name="node"></param>
/// <param name="value"></param>
/// <returns></returns>
public bool Main(HtmlNode node, string value)
{
    // 以下の文字列の場合は処理中断
    if (value.StartsWith("#")) { return false; }
    if (value.Contains("javascript:void(0)")) { return false; }

    // LazyLoadを削除する処理
    removeLazyLoad(node);

    return true;
}