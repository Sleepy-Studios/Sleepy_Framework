
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;


namespace cfg.Fight
{
public partial class TbRank
{
    private readonly System.Collections.Generic.Dictionary<int, Fight.Rank> _dataMap;
    private readonly System.Collections.Generic.List<Fight.Rank> _dataList;
    
    public TbRank(JSONNode _buf)
    {
        _dataMap = new System.Collections.Generic.Dictionary<int, Fight.Rank>();
        _dataList = new System.Collections.Generic.List<Fight.Rank>();
        
        foreach(JSONNode _ele in _buf.Children)
        {
            Fight.Rank _v;
            { if(!_ele.IsObject) { throw new SerializationException(); }  _v = global::cfg.Fight.Rank.DeserializeRank(_ele);  }
            _dataList.Add(_v);
            _dataMap.Add(_v.RankId, _v);
        }
    }

    public System.Collections.Generic.Dictionary<int, Fight.Rank> DataMap => _dataMap;
    public System.Collections.Generic.List<Fight.Rank> DataList => _dataList;

    public Fight.Rank GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public Fight.Rank Get(int key) => _dataMap[key];
    public Fight.Rank this[int key] => _dataMap[key];

    public void ResolveRef(Tables tables)
    {
        foreach(var _v in _dataList)
        {
            _v.ResolveRef(tables);
        }
    }

}

}

