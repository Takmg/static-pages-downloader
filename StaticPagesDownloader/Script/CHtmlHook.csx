using CSScriptLib;
using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

/// <summary>
/// 
/// </summary>
/// <param name="targetUri"></param>
/// <param name="htmlDoc"></param>
/// <returns></returns>
public bool Main(Uri targetUri, HtmlDocument htmlDoc)
{
    // baseノードを削除するサンプル
    //htmlDoc.DocumentNode
    //       .SelectNodes($"//base")?
    //       .Select(e => new { e.ParentNode, Node = e })
    //       .ForEach(e => e.ParentNode.RemoveChild(e.Node));

    return true;
}