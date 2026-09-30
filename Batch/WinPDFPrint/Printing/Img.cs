using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinPDFPrint.Printing
{
    internal class Img
    {
        static public void PrtImg(PdfDocument _myCanvas, Image _img)
        {
            RectangleF rcPic = new RectangleF(_myCanvas.CurrentX, _myCanvas.CurrentY, _img.Width, _img.Height);
            _myCanvas.DrawImage(_img, rcPic);
            _myCanvas.CurrentX += _img.Width;
            _myCanvas.CurrentY += _img.Height;
        }
    }
}
