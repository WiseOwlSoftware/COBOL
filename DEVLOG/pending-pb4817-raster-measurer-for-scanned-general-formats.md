## A raster measurer for scanned general formats (PB4817)

The 1985 PDF is a scan: a general-format page has no vector drawing, so `figure_geometry.py` and `figure_extract.py`
found nothing and the 2023 pipeline could not run on it. `scripts/spec/figure_raster.py` measures the same things from
pixels and returns them in the shapes `figure_geometry.classify_page` and `figure_extract.extract` return, so
`render_figure.py` and the audits take its `classify_page` and `extract` unchanged.

Fix shape: render at 400 dpi, binarize (Otsu), deskew by the shear that sharpens the rows of ink, label connected
components, then read stems (vertical runs as tall as their component), feet (runs anchored on a stem's end rows that
stop at the next stem, so a bar never adopts the bracket's foot, the PB3530 failure at pixel scale), braces (tall
components with straight, aligned halves and a protruding point; a parenthesis is listed as unclassified), underlines
(thin bar components) and words (component boxes grouped by position). The words' text is never read from the image or
the OCR layer: the caller passes the converted page's words per line and `align` pairs them with the boxes; a line
whose counts differ is a reported disagreement and its words keep text None. The underline tests are
`figure_extract`'s own, reused. `--calibrate` writes side-by-side check images.

The self-test draws planted pages (words with one underline, SEND's bracket-bars-bracket with the 0.70 pt gap, a bar
touching a foot, nested braces and brackets, a parenthesis) with a 0.1 to 0.3 degree skew, speckle and dropped pixels,
and asserts the exact structure. Nothing was run against the licensed PDFs (PB4812's standing requirement). The
self-test needs numpy, so the pinned `pip install` lines of the CI self-test jobs gain `numpy==2.5.4`.

Review round 1: a brace is not a stem. `figure_geometry` has no brace stems (braces are Symbol-font glyph pieces in a
vector page), so `render_figure` drew a measured brace as an optional bracket. The measurer now keeps braces in
`RasterPage.braces` and `extract` returns each as the three glyph pieces (top hook, point, bottom hook) that
`render_figure.GLYPH_PIECES` decodes; the self-test renders `{ ALFA / BETA }` through `render_figure.build`. An
underlined word whose components do not pair with its letters is now a reported disagreement.
