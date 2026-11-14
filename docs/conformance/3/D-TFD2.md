- **D-TFD2 — the date/time format literals are case-SENSITIVE, so the §2023:15.92.4 NOTE's own program does not
  compile (§2023:15.3.1.2; kb/Work PB255).** §2023:15.3.1.2 is normative about case — "four **uppercase** 'Y' characters
  representing the year subfield; two **uppercase** 'M' characters …" (`cite.py --check 15.3.1.2 "four uppercase"`
  → OK) — while §2023:15.92.4's NOTE writes `FUNCTION TEST-FORMATTED-DATETIME ("yyyymmdd", A-DATE)` in lowercase. A
  NOTE is not normative, so **WiseOwl COBOL follows §2023:15.3.1.2 and rejects a lowercase format**: `CobolDate.Tokenize`
  matches uppercase only and `DateTimeFormatGrammar` holds uppercase literals, so the reference draws the
  bind-time format-kind diagnostic. Recorded because every fixture in the corpus silently spells the NOTE's
  examples in UPPERCASE, and an unwritten substitution reads as agreement with the NOTE.
