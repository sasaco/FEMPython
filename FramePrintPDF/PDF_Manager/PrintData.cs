using Newtonsoft.Json.Linq;
using PDF_Manager.Printing;
using PDF_Manager.Printing.Diagram3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager
{
    class PrintData
    {
        // classをまとめてここに代入する．
        public Dictionary<string, IPrintable> printDatas = new Dictionary<string, IPrintable>();
        public Dictionary<string, object> printScreens =  new Dictionary<string, object>();
        public Dictionary<string, IPrintable> printLoadDiagram =  new Dictionary<string, IPrintable>();
        public List<string> keyCal = new List<string>();
        public bool hasPrintInputData = false;
        public bool hasPrintCalculation = false;

        /// <summary>
        /// コンストラクタ　印刷するためのデータを集計する 
        /// </summary>
        /// <param name="data"></param>
        public PrintData(Dictionary<string, object> data)
        {
            // ver2 以下のソフトで印刷する場合は null
            if (data.ContainsKey("ver"))
                this.ver = data["ver"].ToString();
            else
                this.ver =null;

            // 2次元か3次元かを記憶
            if (data.ContainsKey("dimension"))
                this.dimension = Int32.Parse(data["dimension"].ToString());
            else
                this.dimension = 3;

            // 言語を記憶
            if (data.ContainsKey("language"))
                this.language = data["language"].ToString();
            else
                this.language = "ja";

            // ペーパサイズ
            if (data.ContainsKey("pageSize"))
                this.pageSize = data["pageSize"].ToString();
            else
                this.pageSize = "A4";

            // ペーパ向き
            if (data.ContainsKey("pageOrientation"))
                this.pageOrientation = data["pageOrientation"].ToString();
            else
                this.pageOrientation = "Vertical"; // or Horizontal

            // タイトル
            if (data.ContainsKey("title"))
                this.title = data["title"].ToString();
            else
                this.title = null;

            // node
            this.printDatas.Add(InputNode.KEY, new InputNode(data));
            // element
            this.printDatas.Add(InputElement.KEY, new InputElement(data));
            // member
            this.printDatas.Add(InputMember.KEY, new InputMember(data));
            // rigid
            this.printDatas.Add(InputRigid.KEY, new InputRigid(data));
            // fixnode
            this.printDatas.Add(InputFixNode.KEY, new InputFixNode(data));
            // joint
            this.printDatas.Add(InputJoint.KEY, new InputJoint(data));
            // notice_points
            this.printDatas.Add(InputNoticePoints.KEY, new InputNoticePoints(data));
            // fixmember
            this.printDatas.Add(InputFixMember.KEY, new InputFixMember(data));
            // shell
            this.printDatas.Add(InputShell.KEY, new InputShell(data));
            // load
            //基本荷重
            var _load_name = new InputLoadName(data);
            this.printDatas.Add(InputLoadName.KEY, _load_name);
            //実荷重
            this.printDatas.Add(InputLoad.KEY, new InputLoad(data, _load_name.loadnames.Keys.ToList()));
            // define
            this.printDatas.Add(InputDefine.KEY, new InputDefine(data));
            // combine 
            this.printDatas.Add(InputCombine.KEY, new InputCombine(data));
            // pickup
            this.printDatas.Add(InputPickup.KEY, new InputPickup(data));
            this.printDatas.Add(BridgeLoadTables.KEY, new BridgeLoadTables(data));

            // disg
            this.printDatas.Add(ResultDisg.KEY, new ResultDisg(data));
            if (data.ContainsKey(ResultDisg.KEY + "Name"))
                keyCal.Add(ResultDisg.KEY + "Name");

            // disgcombine
            this.printDatas.Add(ResultDisgCombine.KEY, new ResultDisgCombine(data));
            if (data.ContainsKey(ResultDisgCombine.KEY + "Name"))
                keyCal.Add(ResultDisgCombine.KEY + "Name");

            // disgPickup
            this.printDatas.Add(ResultDisgPickup.KEY, new ResultDisgPickup(data));
            if (data.ContainsKey(ResultDisgPickup.KEY + "Name"))
                keyCal.Add(ResultDisgPickup.KEY + "Name");

            // fsec
            this.printDatas.Add(ResultFsec.KEY, new ResultFsec(data));
            if (data.ContainsKey(ResultFsec.KEY + "Name"))
                keyCal.Add(ResultFsec.KEY + "Name");

            // fseccombine
            this.printDatas.Add(ResultFsecCombine.KEY, new ResultFsecCombine(data));
            if (data.ContainsKey(ResultFsecCombine.KEY + "Name"))
                keyCal.Add(ResultFsecCombine.KEY + "Name");

            // fsecPickup
            this.printDatas.Add(ResultFsecPickup.KEY, new ResultFsecPickup(data));
            if (data.ContainsKey(ResultFsecPickup.KEY + "Name"))
                keyCal.Add(ResultFsecPickup.KEY + "Name");

            // reac
            this.printDatas.Add(ResultReac.KEY, new ResultReac(data));
            if (data.ContainsKey(ResultReac.KEY + "Name"))
                keyCal.Add(ResultReac.KEY + "Name");

            // reaccombine
            this.printDatas.Add(ResultReacCombine.KEY, new ResultReacCombine(data));
            if (data.ContainsKey(ResultReacCombine.KEY + "Name"))
                keyCal.Add(ResultReacCombine.KEY + "Name");

            // reacPickup
            this.printDatas.Add(ResultReacPickup.KEY, new ResultReacPickup(data));
            if (data.ContainsKey(ResultReacPickup.KEY + "Name"))
                keyCal.Add(ResultReacPickup.KEY + "Name");

            // 荷重図
            if (data.ContainsKey(DiagramInput.KEY))
                this.printDatas.Add(DiagramInput.KEY, new DiagramInput(data));
            // 断面力図
            if (data.ContainsKey(DiagramResult.KEY))
                this.printDatas.Add(DiagramResult.KEY, new DiagramResult(data));

            if (data.ContainsKey(ResultGraphical.KEY))
            {
                this.printLoadDiagram.Add(ResultGraphical.KEY, new ResultGraphical(data));
            }
           

            //printscreens: TEMPORARY LIST OF PRINT SCREENS - DICTIONARY CHILD PRINTDATA;
            var printLoad = "PrintLoad";
            if (data.ContainsKey(printLoad))
            {
                //re-get key data from father print data
                var printLoadData = JObject.FromObject(data[printLoad]).ToObject<Dictionary<string, object>>();
                printLoadData.Add("dimension", dimension);
                printLoadData.Add("language", language);
                printLoadData.Add("ver", ver);
                printLoadData.Add(InputNode.KEY, data[InputNode.KEY]);
                printLoadData.Add(InputMember.KEY, data[InputMember.KEY]);
                if (!printLoadData.ContainsKey("pageOrientation"))
                    printLoadData.Add("pageOrientation", pageOrientation);

                printLoadData.Add(InputElement.KEY, data[InputElement.KEY]);
                if (data.ContainsKey(InputFixMember.KEY))
                    printLoadData.Add(InputFixMember.KEY, data[InputFixMember.KEY]);
                if(data.ContainsKey(InputFixNode.KEY))
                    printLoadData.Add(InputFixNode.KEY, data[InputFixNode.KEY]);
                if (data.ContainsKey(InputLoad.KEY))
                    printLoadData.Add(InputLoad.KEY, data[InputLoad.KEY]);

                this.printScreens.Add(printLoad, printLoadData);
            }

            var printDiagram = "PrintDiagram";
            if (data.ContainsKey(printDiagram))
            {
                //re-get key data from father print data
                var printDiagramData = JObject.FromObject(data[printDiagram]).ToObject<Dictionary<string, object>>();
                printDiagramData.Add("dimension", dimension);
                printDiagramData.Add("language", language);
                printDiagramData.Add("ver", ver);
                printDiagramData.Add(InputNode.KEY, data[InputNode.KEY]);
                printDiagramData.Add(InputMember.KEY, data[InputMember.KEY]);
                if (!printDiagramData.ContainsKey("pageOrientation"))
                    printDiagramData.Add("pageOrientation", pageOrientation);

                if (data.ContainsKey(InputLoad.KEY))
                    printDiagramData.Add(InputLoad.KEY, data[InputLoad.KEY]);
                if (data.ContainsKey(ResultFsec.KEY))
                    printDiagramData.Add(ResultFsec.KEY, data[ResultFsec.KEY]);

                this.printScreens.Add(printDiagram, printDiagramData);
            }

            var printCombDiagram = "CombPrintDiagram";
            if (data.ContainsKey(printCombDiagram))
            {
                //re-get key data from father print data
                var printCombDiagramData = JObject.FromObject(data[printCombDiagram]).ToObject<Dictionary<string, object>>();
                printCombDiagramData.Add("dimension", dimension);
                printCombDiagramData.Add("language", language);
                printCombDiagramData.Add("ver", ver);
                printCombDiagramData.Add(InputNode.KEY, data[InputNode.KEY]);
                printCombDiagramData.Add(InputMember.KEY, data[InputMember.KEY]);
                if (!printCombDiagramData.ContainsKey("pageOrientation"))
                    printCombDiagramData.Add("pageOrientation", pageOrientation);

                if (data.ContainsKey(InputCombine.KEY))
                    printCombDiagramData.Add(InputCombine.KEY, data[InputCombine.KEY]);
                if (data.ContainsKey(ResultFsecCombine.KEY))
                    printCombDiagramData.Add(ResultFsecCombine.KEY, data[ResultFsecCombine.KEY]);

                this.printScreens.Add(printCombDiagram, printCombDiagramData);

            }

            var printPickDiagram = "PickPrintDiagram";
            if (data.ContainsKey(printPickDiagram))
            {
                //re-get key data from father print data
                var printPickDiagramData = JObject.FromObject(data[printPickDiagram]).ToObject<Dictionary<string, object>>();
                printPickDiagramData.Add("dimension", dimension);
                printPickDiagramData.Add("language", language);
                printPickDiagramData.Add("ver", ver);
                printPickDiagramData.Add(InputNode.KEY, data[InputNode.KEY]);
                printPickDiagramData.Add(InputMember.KEY, data[InputMember.KEY]);
                if (!printPickDiagramData.ContainsKey("pageOrientation"))
                    printPickDiagramData.Add("pageOrientation", pageOrientation);

                if (data.ContainsKey(InputPickup.KEY))
                    printPickDiagramData.Add(InputPickup.KEY, data[InputPickup.KEY]);
                if (data.ContainsKey(ResultFsecPickup.KEY))
                    printPickDiagramData.Add(ResultFsecPickup.KEY, data[ResultFsecPickup.KEY]);

                this.printScreens.Add(printPickDiagram, printPickDiagramData);
            }

            //MEMORY HAS CHECKING PRINT "INPUTDATA"
            if (data.ContainsKey("hasPrintInputData"))
                this.hasPrintInputData = bool.Parse(data["hasPrintInputData"].ToString() ?? "false");
            else
                this.hasPrintInputData = false;

            //MEMORY HAS CHECKING PRINT "RESULT CALCULATION"
            if (data.ContainsKey("hasPrintCalculation"))
                this.hasPrintCalculation = bool.Parse(data["hasPrintCalculation"].ToString() ?? "false");
            else
                this.hasPrintCalculation = false;
        }

        #region 他のモジュールのヘルパー関数

        public string ver { get; }

        /// <summary>
        /// ver 2.0.0 より古いのか？
        /// </summary>
        public bool isOlderVer2
        {
            get
            {
                if (this.ver == null)
                    return true;
                return false;
            }
        }


        /// <summary>
        /// 2次元モードか3次元モードか？
        /// </summary>
        public int dimension { get; }

        public string language { get; }

        public string pageSize { get; }

        public string pageOrientation { get; }

        public string title { get; }

        #endregion
    }
}
