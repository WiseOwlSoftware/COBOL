## The raw 1985 pages are LF

The batch collector had written 959 of the raw 1985 pages with CRLF, Python's text-mode default on Windows, while the five local pages were LF. Under the project's LF standard, those pages were converted to LF in place, and both `batch1985.py` and `local1985.py` now write with an explicit LF. Every raw page is LF, so the converter (PB4816) needs no normalisation step. kb/Work PB4812 records it.
