using Convert_Manager.FrameWebForJS;
using Newtonsoft.Json;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class ConvertManager
{
    public Dictionary<string, string> wdata = new Dictionary<string, string>();

    private Dictionary<string, object> result = new Dictionary<string, object>();


    // 節点
    public node _node;
    // 支点
    public fix_node _fix_node;
    // 部材 と 材料
    public member _member;
    // バネ
    public fix_member _fix_member;
    // 結合
    public joint _joint;
    // 着目点
    public notice_points _notice_point;
    // 荷重
    public load _load;
    // DEFINE
    public define _define;
    // 組合せ
    public combine _combine;
    // PickUP
    public pickup _pickup;
    // 剛域
    public gouiki _gouiki;


    public ConvertManager(Stream BaseStream)
    {

        using (ArchiveFile archiveFile = new ArchiveFile(BaseStream, SevenZipFormat.Lzh))
        {
            foreach (Entry ent in archiveFile.Entries)
            {
                var ms = new MemoryStream();
                ent.Extract(ms);
                string str = Encoding.GetEncoding(932).GetString(ms.ToArray()); // MemoryStreamをstringに変換する
                wdata.Add(ent.FileName, str);
            }
        }

        // 節点
        _node = new node(wdata);
        // 支点
        _fix_node = new fix_node(wdata);
        // 部材 と 材料
        _member = new member(wdata);
        // バネ
        _fix_member = new fix_member(wdata);
        // 結合
        _joint = new joint(wdata);
        // 着目点
        _notice_point = new notice_points(wdata);
        // 荷重
        _load = new load(wdata);
        // DEFINE
        _define = new define(wdata);
        // 組合せ
        _combine = new combine(wdata);
        // PickUP
        _pickup = new pickup(wdata);



        /// 組み換え処理
        // 杭バネは元の部材番号で追加し、剛域分割時に区間とJ端を引き継ぐ。
        pile_spring.Apply(wdata, _node, _member, _fix_node, _fix_member);
        // 剛域
        _gouiki = new gouiki(wdata);
        _gouiki.exChange(_node, _member, _fix_node, _fix_member, 
                            _joint, _notice_point, _load);



        /// 書き出し
        // 節点
        result.Add(node.KEY, _node.GetNode());
        // 支点
        result.Add(fix_node.KEY, _fix_node.GetFixNode());
        // 部材
        result.Add(member.mKEY, _member.GetMember());
        // 材料
        result.Add(element.eKEY, _member.GetElement());
        // 着目点
        result.Add(notice_points.KEY, _notice_point.GetNoticePoint());
        // バネ
        result.Add(fix_member.KEY, _fix_member.GetFixMember());
        // 結合
        result.Add(joint.KEY, _joint.GetJoint());
        // 荷重
        result.Add(load.KEY, _load.GetLoad());
        // DEFINE
        result.Add(define.KEY, _define.GetDefine()); 
        // 組合せ
        result.Add(combine.KEY, _combine.GetCombine());
        // PickUP
        result.Add(pickup.KEY, _pickup.GetPickup());

    }

    // json 文字列を取得
    public string getJsonString()
    {
        //jsonに変換する
        string json = JsonConvert.SerializeObject(result);
        return json;
    }




}



