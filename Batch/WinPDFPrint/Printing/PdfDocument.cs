using C1.C1Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinPDFPrint.Printing
{
    internal class PdfDocument : C1PdfDocument
    {
        public PdfDocument()
        {
            this.SetFont(PdfDocument.DefaultFontName, PdfDocument.DefaultFontSize, Brushes.Black);
            _CurrentX = 0;
            _CurrentY = 0;
            rootX1 = 0;
            rootX2 = float.MaxValue;
            rootY1 = 0;
            rootY2 = float.MaxValue;
        }
        private float _CurrentX;
        private float _CurrentY;
        public float CurrentX{
            set { this._CurrentX = value; }
            get { return this._CurrentX; }
        }
        public float CurrentY{
            set { this._CurrentY = value; }
            get { return this._CurrentY; }
        }

        public RectangleF Margine = new RectangleF();

        public float rootX1;
        public float rootX2;
        public float rootY1;
        public float rootY2;

        public static string DefaultFontName = "ＭＳ 明朝";//"Arial";//"MS UI Gothic";//
        public static float DefaultFontSize = 10;
        private SizeF _referenceSize;
        public SizeF referenceSize{
            get {
                if (_referenceSize.IsEmpty)
                    _referenceSize = base.MeasureString("H", this.font);
                return _referenceSize;
            } 
        }

        public Font font;
        public void SetFont(string sFontName, float dFontSize, Brush fontColor)
        {
            // Create a font
            if (sFontName != "")
                this._FontName = sFontName;

            if (dFontSize > 0)
                this._FontSize = dFontSize;

            if (fontColor != null)
                this.fontColor = fontColor;

            this.font = new Font(this.FontName, this.FontSize);

            this._referenceSize = base.MeasureString("H", this.font);
        }

        public Brush fontColor;

        private string _FontName;
        public string FontName
        {
            get { return this._FontName; }
            set
            {
                this._FontName = value;
                SetFont(this._FontName, this._FontSize, this.fontColor);
            }
        }
        private float _FontSize;
        public float FontSize
        {
            get { return this._FontSize; }
            set
            {
                this._FontSize = value;
                SetFont(this._FontName, this._FontSize, this.fontColor);
            }
        }

        public SizeF MeasureString(string text)
        {
            SizeF result = base.MeasureString(text, this.font);
            return result;
        }


    }
}
