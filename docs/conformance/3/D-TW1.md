- **D-TW1 — a period, comma or semicolon written immediately before a closing pseudo-text delimiter is a
  SEPARATOR, so `==STOP RUN.==` holds the text-words `STOP` `RUN` `.`** (kb/Work PB1350). §2023:8.3.5 2)/3) make those
  characters separators only "when followed by a space", and read literally `RUN.==` would form the one text-word
  `RUN.` — a pseudo-text that no source text can ever match, since source text writes the sentence period before a
  space or a line end. But §2023:8.3.5 8) lets the separator space OPTIONALLY precede every separator but three, and the
  closing pseudo-text delimiter is not one of the three, so `==STOP RUN.==` and `==STOP RUN. ==` are the same
  pseudo-text written with and without an optional space and must form the same text-words. Everywhere else the
  rule is applied as printed — `ZZ.ZZ`, `Z,ZZ9` and `X.COPY` are single text-words. Written once, in
  `TextWordScanner.SeparatorFollows`; witnessed by `unit:TextManipulationTextWordTests.ReplaceStatement_PeriodInsidePseudoText`
  and `conformance:cases/7.2.2.5-Text-words/pb1350_text_words_85` (`[ZZZ]`).
