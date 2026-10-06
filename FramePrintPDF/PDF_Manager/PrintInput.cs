using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using PDF_Manager.Printing.Diagram3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public class PrintInput
{
    private PrintData data;

    public PrintInput(string jsonString, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // データを読み込む
        JObject data = JObject.Parse(jsonString);
        cancellationToken.ThrowIfCancellationRequested();
        var value = data.ToObject<Dictionary<string, object>>();
        cancellationToken.ThrowIfCancellationRequested();
        //　準備のためのclassの呼び出し
        this.data = new PrintData(value);
    }

    /// <summary>
    /// インプットデータの印刷PDFを生成する
    /// </summary>
    public void createPDF()
    {
        //  PDF出力のためのclassの呼び出し
        //  整形したデータを送る
        using var mc = PrintInput.printPDF(this.data);

        // PDFファイルを生成する
        mc.SavePDF();
    }
    public void createPDF(string filename)
    {
        //  PDF出力のためのclassの呼び出し
        //  整形したデータを送る
        using var mc = PrintInput.printPDF(this.data);

        // PDFファイルを生成する
        mc.SavePDF(filename);
    }

    /// <summary>
    /// PDFのBase64コードを返す
    /// </summary>
    /// <returns></returns>
    public string getPdfSource()
    {
        //  PDF出力のためのclassの呼び出し
        //  整形したデータを送る
        using var mc = PrintInput.printPDF(this.data);

        // PDF を Byte型に変換
        var b = mc.GetPDFBytes();

        // Byte型配列をBase64文字列に変換
        string str = Convert.ToBase64String(b);

        // PDFファイルを生成する
        return str;
    }

    /// <summary>Generate PDF bytes directly for in-process desktop printing.</summary>
    public byte[] GetPdfBytes(int maxPages, int maxBytes, CancellationToken cancellationToken = default)
    {
        using var document = printPDF(data, maxPages, cancellationToken);
        return document.GetPDFBytes(maxPages, maxBytes);
    }

    /// <summary>
    /// PDF を生成する
    /// </summary>
    /// <param name="data"></param>
    private static PdfDocument printPDF(PrintData data, int maxPages = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        PdfDocument mc = null;
        try
        {
            // PDF ページを準備する
            int indexPage = 1;
            var printScreens = data.printScreens;
            var printLoad3D = data.printLoadDiagram;

            if (data.dimension == 2)
            {
                //For each passing section, check if there is data in the newpage.
                //Based on prevIndexPage to determine whether each printing section has data printed or not.
                var hasPreviousData = false;
                var prevIndexPage = indexPage;

                mc = new PdfDocument(data, ref indexPage, maxPages, cancellationToken);

                //CHECK HAS PRINT INPUT DATA
                if (data.hasPrintInputData)
                {
                    //入力データ
                    //  格点
                    data.printDatas[InputNode.KEY].printPDF(mc, data, ref indexPage);
                    // 部材
                    data.printDatas[InputMember.KEY].printPDF(mc, data, ref indexPage);
                    // 剛域データ
                    data.printDatas[InputRigid.KEY].printPDF(mc, data, ref indexPage);
                    // 材料
                    data.printDatas[InputElement.KEY].printPDF(mc, data, ref indexPage);
                    // 支点
                    data.printDatas[InputFixNode.KEY].printPDF(mc, data, ref indexPage);
                    // 結合
                    data.printDatas[InputJoint.KEY].printPDF(mc, data, ref indexPage);
                    // 着目点
                    data.printDatas[InputNoticePoints.KEY].printPDF(mc, data, ref indexPage);
                    // バネ
                    data.printDatas[InputFixMember.KEY].printPDF(mc, data, ref indexPage);
                    // シェル
                    data.printDatas[InputShell.KEY].printPDF(mc, data, ref indexPage);
                    // 荷重名称
                    data.printDatas[InputLoadName.KEY].printPDF(mc, data, ref indexPage);
                    // 荷重強度 
                    data.printDatas[InputLoad.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせDefine
                    data.printDatas[InputDefine.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせCombine
                    data.printDatas[InputCombine.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせピックアップ
                    data.printDatas[InputPickup.KEY].printPDF(mc, data, ref indexPage);

                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }

                //CHECK HAS PRINT CALCULATION RESULT
                if (data.hasPrintCalculation)
                {
                    // 計算結果データ
                    // 変位量
                    if (data.keyCal.Any(x => x.Contains(ResultDisg.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultDisg.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // 反力
                    if (data.keyCal.Any(x => x.Contains(ResultReac.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultReac.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // 断面力
                    if (data.keyCal.Any(x => x.Contains(ResultFsec.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultFsec.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }

                    // 組み合わせ変位量
                    if (data.keyCal.Any(x => x.Contains(ResultDisgCombine.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultDisgCombine.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // 組み合わせ反力
                    if (data.keyCal.Any(x => x.Contains(ResultReacCombine.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultReacCombine.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // 組み合わせ断面力
                    if (data.keyCal.Any(x => x.Contains(ResultFsecCombine.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultFsecCombine.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }

                    // ピックアップ変位量
                    if (data.keyCal.Any(x => x.Contains(ResultDisgPickup.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultDisgPickup.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // ピックアップ反力
                    if (data.keyCal.Any(x => x.Contains(ResultReacPickup.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultReacPickup.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                    // ピックアップ断面力
                    if (data.keyCal.Any(x => x.Contains(ResultFsecPickup.KEY + "Name")))
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }
                        data.printDatas[ResultFsecPickup.KEY].printPDF(mc, data, ref indexPage);
                        if (prevIndexPage != indexPage)
                        {
                            prevIndexPage = indexPage;
                            hasPreviousData = true;
                        }
                        else
                        {
                            hasPreviousData = false;
                        }
                    }
                }

                if (printScreens.Count > 0)
                {
                    mc.NewPage(ref indexPage);
                }
                
                //CHECK FOR PRINT SCREENS
                for (int i = 0; i < printScreens.Count; i++)
                {
                    var key = printScreens.ElementAt(i).Key;
                    if (key.ToLower().Equals("printload"))
                    {
                        if (hasPreviousData) mc.NewPage(ref indexPage);
                        var target = JObject.FromObject(printScreens.ElementAt(i).Value).ToObject<Dictionary<string, object>>();
                        var dataChild = new PrintData(target);
                        if (dataChild.printDatas.ContainsKey(DiagramInput.KEY))
                        {
                            DiagramInput diaLoad = (DiagramInput)dataChild.printDatas[DiagramInput.KEY];
                            diaLoad.printPDF(mc, dataChild, ref indexPage);                 // 格点
                        }
                        hasPreviousData = true;
                        prevIndexPage = indexPage;
                    }
                    else
                    {
                        if (hasPreviousData)
                        {
                            mc.NewPage(ref indexPage);
                            prevIndexPage = indexPage;
                        }

                        var target = JObject.FromObject(printScreens.ElementAt(i).Value).ToObject<Dictionary<string, object>>();
                        var dataChild = new PrintData(target);

                        //断面力図
                        if (dataChild.printDatas.ContainsKey(DiagramResult.KEY))
                        {
                            DiagramResult diaFsec = (DiagramResult)dataChild.printDatas[DiagramResult.KEY];
                            diaFsec.printPDF(mc, dataChild, ref indexPage);

                            if (prevIndexPage != indexPage)
                            {
                                prevIndexPage = indexPage;
                                hasPreviousData = true;
                            }
                            else
                            {
                                hasPreviousData = false;
                            }
                        }
                    }
                }
                return mc;
            }
            else
            {
                //For each passing section, check if there is data in the newpage.
                //Based on prevIndexPage to determine whether each printing section has data printed or not.

                //CHECK HAS PREVIOUS DATA TO NEXT NEW PAGE
                var hasPreviousData = false;
                var prevIndexPage = indexPage;

                //SAME AS NOMARL PRINT
                mc = new PdfDocument(data, ref indexPage, maxPages, cancellationToken);
                // 荷重図
                if (data.printDatas.ContainsKey(DiagramInput.KEY))
                {
                    DiagramInput diaLoad = (DiagramInput)data.printDatas[DiagramInput.KEY];
                    diaLoad.printPDF(mc, data, ref indexPage);                        // 格点
                    return mc; // 荷重図の指定があったらその他の出力はしない
                }
                //断面力図
                if (data.printDatas.ContainsKey(DiagramResult.KEY))
                {
                    DiagramResult diaFsec = (DiagramResult)data.printDatas[DiagramResult.KEY];
                    diaFsec.printPDF(mc, data, ref indexPage);                        // 格点
                    return mc; // 荷重図の指定があったらその他の出力はしない
                }

                if (data.hasPrintInputData)
                {
                    //入力データ
                    //  格点
                    data.printDatas[InputNode.KEY].printPDF(mc, data, ref indexPage);
                    // 部材
                    data.printDatas[InputMember.KEY].printPDF(mc, data, ref indexPage);
                    // 剛域データ
                    data.printDatas[InputRigid.KEY].printPDF(mc, data, ref indexPage);
                    // 材料
                    data.printDatas[InputElement.KEY].printPDF(mc, data, ref indexPage);
                    // 支点
                    data.printDatas[InputFixNode.KEY].printPDF(mc, data, ref indexPage);
                    // 結合
                    data.printDatas[InputJoint.KEY].printPDF(mc, data, ref indexPage);
                    // 着目点
                    data.printDatas[InputNoticePoints.KEY].printPDF(mc, data, ref indexPage);
                    // バネ
                    data.printDatas[InputFixMember.KEY].printPDF(mc, data, ref indexPage);
                    // シェル
                    data.printDatas[InputShell.KEY].printPDF(mc, data, ref indexPage);
                    // 荷重名称
                    data.printDatas[InputLoadName.KEY].printPDF(mc, data, ref indexPage);
                    // 荷重強度 
                    data.printDatas[InputLoad.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせDefine
                    data.printDatas[InputDefine.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせCombine
                    data.printDatas[InputCombine.KEY].printPDF(mc, data, ref indexPage);
                    // 組み合わせピックアップ
                    data.printDatas[InputPickup.KEY].printPDF(mc, data, ref indexPage);

                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }

                var bridgeTables = (BridgeLoadTables)data.printDatas[BridgeLoadTables.KEY];
                if (bridgeTables.HasData)
                {
                    bridgeTables.printPDF(mc, data, ref indexPage);
                    hasPreviousData = true;
                    prevIndexPage = indexPage;
                }

                // 計算結果データ
                // 変位量
                if (data.keyCal.Any(x => x.Contains(ResultDisg.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultDisg.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // 反力
                if (data.keyCal.Any(x => x.Contains(ResultReac.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultReac.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // 断面力
                if (data.keyCal.Any(x => x.Contains(ResultFsec.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultFsec.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }

                // 組み合わせ変位量
                if (data.keyCal.Any(x => x.Contains(ResultDisgCombine.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultDisgCombine.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // 組み合わせ反力
                if (data.keyCal.Any(x => x.Contains(ResultReacCombine.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultReacCombine.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // 組み合わせ断面力
                if (data.keyCal.Any(x => x.Contains(ResultFsecCombine.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultFsecCombine.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }

                // ピックアップ変位量
                if (data.keyCal.Any(x => x.Contains(ResultDisgPickup.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultDisgPickup.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // ピックアップ反力
                if (data.keyCal.Any(x => x.Contains(ResultReacPickup.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultReacPickup.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                // ピックアップ断面力
                if (data.keyCal.Any(x => x.Contains(ResultFsecPickup.KEY + "Name")))
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printDatas[ResultFsecPickup.KEY].printPDF(mc, data, ref indexPage);
                    if (prevIndexPage != indexPage)
                    {
                        prevIndexPage = indexPage;
                        hasPreviousData = true;
                    }
                    else
                    {
                        hasPreviousData = false;
                    }
                }
                if(printLoad3D != null &&  printLoad3D.Count > 0)
                {
                    if (hasPreviousData)
                    {
                        mc.NewPage(ref indexPage);
                        prevIndexPage = indexPage;
                    }
                    data.printLoadDiagram[ResultGraphical.KEY].printPDF(mc, data, ref indexPage);
                }

                return mc;
            }
        }
        catch
        {
            mc?.Dispose();
            throw;
        }
    }

}
