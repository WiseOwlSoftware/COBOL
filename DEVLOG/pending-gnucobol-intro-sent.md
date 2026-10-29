## PB3838 tier 6: the introduction to the GnuCOBOL maintainers is sent

The owner emailed the introduction to `bug-gnucobol@gnu.org` on 2026-10-29. It describes the differential testing and
the 78 records:
- 21 are already on the tracker and will not be re-filed.
- One is fixed in 3.3-dev r5751.
- About 20 reports remain after grouping.

The email discloses the AI assistance, asks the maintainers how they want the reports, and offers patches and the FSF
copyright assignment. PB3838 records it, with the grouping its count rests on.

Its example is PB3887, chained exponentiation, in the three-operand form of night 1's lost witness: `2 ** 3 ** 4` into
`PIC Z(24)9`. GnuCOBOL 3.2.0 and 3.3-dev both print 2 ** 81, 2417851639229258349412352. WiseOwl COBOL prints 4096, the
left-to-right result that §8.8.1.2 rule 3 requires. PB3887 now records that run.

Four notes (PB3885 to PB3888) still said "Not re-run on the current development branch" after the tier-6 triage had
re-run them. The triage's script had corrected two other wordings of that claim but not this one. All four are now
corrected in place, and a sweep of the 78 notes finds no other form of it.
