using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using PdfSharpCore;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using Newtonsoft.Json.Linq;
using PDF_Manager.Printing;
using PdfSharpCore.Utils;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Newtonsoft.Json;

namespace PDF_Manager.Printing
{
    internal class Text
    {
        static public void PrtText(PdfDocument mc, string str, XFont font = null, XStringFormat align = null)
        {
            mc.CheckBudget();
            if (str == null)
                return;

            if (align == null)
                align = XStringFormats.BottomLeft; // 左下起点

            if (font == null)
                font = mc.font_mic; // 明朝

            // 文字列描画
            if (mc.TitleArea.X != 0 && mc.TitleArea.Y != 0)
                mc.gfx.DrawString(str, font, XBrushes.Black, mc.TitleArea, align);
            else
                mc.gfx.DrawString(str, font, XBrushes.Black, mc.currentPos, align);

        }
        static public void PrtTextLoadName(PdfDocument mc, string str, XFont font = null, XStringFormat align = null)
        {
            mc.CheckBudget();
            if (str == null)
                return;

            if (align == null)
                align = XStringFormats.BottomLeft; // 左下起点

            if (font == null)
                font = mc.font_mic; // 明朝

            // 文字列描画
            mc.gfx.DrawString(str, font, XBrushes.Black, new XPoint(mc.currentPos.X, mc.currentPos.Y - 22), align);

        }
        static public void PrtTextLoadDig(PdfDocument mc, string str, XPoint? point = null, XFont font = null, XStringFormat align = null)
        {
            mc.CheckBudget();
            if (str == null)
                return;

            if (align == null)
                align = XStringFormats.BottomLeft; // 左下起点

            if (font == null)
                font = mc.font_mic; // 明朝

            if(point == null)
            {
                // 文字列描画
                if (mc.TitleArea.X != 0 && mc.TitleArea.Y != 0)
                    mc.gfx.DrawString(str, font, XBrushes.Black, mc.TitleArea, align);
                else
                    mc.gfx.DrawString(str, font, XBrushes.Black, mc.currentPos, align);
            }
            else
            {
                mc.gfx.DrawString(str, font, XBrushes.Black, point.Value, align);
            }
            
        }
    }
}
