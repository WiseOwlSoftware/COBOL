## PB4972: the 1985 transcription corrects 19 misprints of the printed standard and lists them in an Errata

The owner decided on 2026-11-08 that `specs/ANSI_COBOL_1985.md` corrects the misprints it inherits from the printed
standard and ends with a section listing every correction. Until then the file transcribed misprints as printed.

The mechanism is 2023's (kb/Work R66). A correction is made in place, flagged with a ⚠ note that quotes the printed
form and names the page, and listed in the closing section. `verify_publishable.check_corrections` now takes the
edition, and `CORRECTIONS_SECTION` names each edition's section: the Addendum for 2023, the Errata for 1985, whose text
already has an Addendum of its own (Amendment 1, Addendum 1). `sweep_figures.py` read a second copy of the 2023 flag
pattern; it now reads `flag_reference`. The 1985 rule catalog's reader did not skip a correction note, as the 2023
reader does; it does now, with a planted case.

A scan for rare words one edit from a common word, and a sweep for every accented or foreign letter (the branch review
found two more that way), gave the candidates. Each was read on the rendered page. Seven are misspellings (suppported,
constitutent, statment twice, coresponding, concernings, upppercase). Twelve are letters the print damaged and the
transcription had reproduced (piovides, _ables, comment-entrıes, BEFCRE, Fil'es, omi-ted, fopting, thac, techñique,
ıeturns, rêport, desçription). BEFCRE sits in INSPECT's general format on page
217, so its figure verdict now carries BEFORE and is decided `neither`. The acknowledgment stays exactly as printed;
Amendment 1's copy of it misprints "Commerical", recorded as D2 and not corrected. Eight catalog rule texts changed.
The KEPT ledger claim for "fopting" written the day before is removed, since the word is now corrected.
