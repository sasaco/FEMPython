using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;

namespace PDF_Manager.Printing.Diagram3D
{
    abstract class PrintBase3dDiagram: IPrintable
    {
        
        protected abstract List<GraphicalOutput> Load();
        public void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            printTitle(mc);
            var result = Load();
            if (result.Count > 0)
            {
                for(int i = 0; i < result.Count; i++)
                {
                    mc.CheckBudget();
                    if (result.Count > 1 && i > 0)
                    {
                        mc.NewPage(ref indexPage);
                        printTitle(mc);
                    }
                    var graphic = result[i];
                    bool bridge = graphic.Mode == "print_bridge_load";
                    XFont font = new XFont("MS Gothic", printManager.FontSize - 3, XFontStyle.Bold);
                   
                    mc.gfx.DrawString(graphic.Title, font, XBrushes.Black, new XPoint(mc.currentPos.X + 50, mc.currentPos.Y + 8));
                    if(graphic.Result.Count > 0)
                    {
                        double distance = 0;
                        for (int p = 0; p < graphic.Result.Count; p++)
                        {
                            mc.CheckBudget();
                            if (graphic.Result.Count > 1 && p % 2 == 0 && p > 0)
                            {
                                mc.NewPage(ref indexPage);
                                printTitle(mc);
                                distance = 0;
                            }
                            var rs = graphic.Result[p];
                            XFont fontTt = new XFont("MS Mincho", printManager.FontSize - 3, XFontStyle.Regular);
                            if (!bridge) mc.gfx.DrawString(rs.title, fontTt, XBrushes.Black, new XPoint(mc.currentPos.X + 50 , mc.currentPos.Y + 30 + distance));


                            if (!bridge) mc.gfx.DrawString(rs.type == null? "" : rs.type, font, XBrushes.Black, new XPoint(mc.currentPos.X + 50 , mc.currentPos.Y + 45 + distance));
                            if (!bridge && !graphic.Mode.Contains("print_load"))
                            {
                                mc.gfx.DrawString($"Max: {rs.max_three}", font, XBrushes.Black, new XPoint(mc.currentPos.X + 50, mc.currentPos.Y + 60 + distance));
                                mc.gfx.DrawString($"Min: {rs.min_three}", font, XBrushes.Black, new XPoint(mc.currentPos.X + 50, mc.currentPos.Y + 75 + distance));
                            }                           

                            XFont fontdisg = new XFont("MS Mincho", 4, XFontStyle.Regular);
                            mc.gfx.DrawString(rs.disgSubInfo1 == null ? "" : rs.disgSubInfo1, fontdisg, XBrushes.Black, new XPoint(mc.currentPos.X + 50, mc.currentPos.Y + 90 + distance));
                            mc.gfx.DrawString(rs.disgSubInfo2 == null ? "" : rs.disgSubInfo2, fontdisg, XBrushes.Black, new XPoint(mc.currentPos.X + 50, mc.currentPos.Y + 95 + distance));

                            const int maxImageBytes = 16 * 1024 * 1024;
                            const long maxImagePixels = 16_000_000;
                            string encoded = rs.src.Replace("data:image/png;base64,", "");
                            if (encoded.Length > ((long)maxImageBytes + 2) / 3 * 4)
                                throw new InvalidOperationException("Diagram image exceeds the byte limit.");
                            byte[] source = Convert.FromBase64String(encoded);
                            if (source.Length > maxImageBytes)
                                throw new InvalidOperationException("Diagram image exceeds the byte limit.");
                            var info = Image.Identify(source);
                            if (info == null || (long)info.Width * info.Height > maxImagePixels)
                                throw new InvalidOperationException("Diagram image exceeds the pixel limit.");
                            using var contents = new MemoryStream(source);
                            using XImage image = XImage.FromStream(() => contents);
                            if (bridge)
                            {
                                double top = mc.currentPos.Y + 35;
                                double availableWidth = mc.currentPageSize.Width - 20;
                                double availableHeight = mc.currentPage.Height.Point - mc.Margine.Bottom - top - 20;
                                double scale = Math.Min(availableWidth / info.Width, availableHeight / info.Height);
                                double width = info.Width * scale;
                                double height = info.Height * scale;
                                mc.gfx.DrawImage(image, mc.Margine.Left + (mc.currentPageSize.Width - width) / 2,
                                    top, width, height);
                            }
                            else mc.gfx.DrawImage(image, mc.currentPos.X + 50, mc.currentPos.Y + 110 + distance, 400, 200);
                            distance += 300;                            
                        }
                    }                   
                        

                }
                
            }
           
           
        }
        public void printTitle(PdfDocument mc)
        {
            DateTime dateTime = DateTime.Now;
            string formattedString = dateTime.ToString("dd/MM/yyyy, HH:mm");
            Text.PrtTextLoadDig(mc, formattedString, new XPoint(mc.currentPos.X - 10, mc.currentPos.Y - 40), mc.font_mic);
            Text.PrtTextLoadDig(mc, "立体骨組構造解析ソフト", new XPoint(mc.currentPos.X + 280, mc.currentPos.Y - 40), mc.font_mic);           
        }
    }    
}
