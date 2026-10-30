# PDF の頁を PNG にする。使い方: uv run --with pymupdf python -I render_pages.py <pdf> <出力フォルダ> <開始頁> <終了頁> [倍率]
import sys
import pymupdf as fitz
doc = fitz.open(sys.argv[1]); out = sys.argv[2]
a, b = int(sys.argv[3]), int(sys.argv[4]); zoom = float(sys.argv[5]) if len(sys.argv) > 5 else 2
for p in range(a, b + 1):
    page = doc[p - 1]
    page.get_pixmap(matrix=fitz.Matrix(zoom, zoom)).save(f"{out}/p{p}.png")
    print(p, page.rect, len(page.get_text()))
