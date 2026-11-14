# WiseOwl COBOL — Conformance & Processor-Dependent Element Disposition

> **STATUS: LIVE conformance record (ISO/IEC 1989:2023 §2023:4.2.16).** This document is the implementor's user
> documentation required by §2023:4.2.6 (processor-dependent elements), §2023:4.2.7 (optional elements), and §2023:4.2.13
> (obsolete elements): it states, for every processor-dependent language element in Annex A.3 and every optional
> facility this compiler does **not** implement, whether support is claimed. Every syntactically-detectable
> unsupported element is NAMED at compile time, on the channel its licence chooses: an Annex A.3
> **processor-dependent** facility is accepted with a named WARNING (**COBOL1778**, §2023:4.2.6
> third paragraph, which MANDATES that warning), and an Annex A.4 **optional module for which no support is
> claimed** is REFUSED with a named ERROR (the **COBOL1560 / 1705 / 1706 / 1707** band, §2023:4.2.7 and Annex
> A.4.1). **COBOL1580 (VALIDATE) is the one element that is A.4 and yet warns**, and it is a CHOICE rather
> than a mandate — see §4 (kb/Work PB709). This document is the
> authoritative catalogue behind both. Default `--std` = COBOL-2023.
