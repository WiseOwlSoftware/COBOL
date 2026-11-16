**2026-11-15, golden-2002-6 integrator: the same question from the 2002-GR-11.6.3-1 side (writer of g2002u-11-3-factory).**
Repros `E:\COBOL-coord\scratch\gl2002\integrate-6\repro\mid_pos.cob` and `mid_ext.cob` (from `lane1\run\g2002u-11-3-factory\mid\`):
class G2MIDC declares `METHOD-ID. ALPHM AS "G2ALPHEXT".` and `METHOD-ID. NATM AS N"G2NATEXT".`; the main program,
`O` typed `OBJECT REFERENCE G2MIDC`, runs `INVOKE O "ALPHM"` (mid_pos) or `INVOKE O "G2ALPHEXT"` (mid_ext). Re-run on a
build of main 5e49d46678 at `--std 2002 --run`: mid_pos is refused, exit 65, `(13,12): error COBOL0825: INVOKE 'O' "ALPHM":
class 'G2MIDC' (and its inheritance chain) does not define an instance method named 'ALPHM'` (and the same for NATM);
mid_ext runs and prints `ALPHANUMERIC AS`, `NATIONAL AS`. §2002:11.6.3 GR1b ("However, literal-1, if specified, is the name
of method that is externalized to the operating environment") with §2002:14.8.22.2 SR4b is the reading this note already
records; the landed witness g2mid01_as_alphanumeric_national invokes by the externalized literal, so 2002-GR-11.6.3-1's
record rests on that reading until this adjudication decides it. No new note.
