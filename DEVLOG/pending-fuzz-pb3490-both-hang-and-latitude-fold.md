## The differential fuzz harness: a hang on both sides is the model's, and the display-form latitude is folded out of the survivors

Before the second overnight run the harness learned the two lessons of the first night's triage (kb/Work PB3490). A program
both compilers run forever was reported as a WiseOwl hang because `classify()` tested our hang flag before theirs; it is now
`BOTH_HANG`, a model-failure class, and three of the first night's eight `WE_HANG` rows move there. And the 615-row
latitude the triage folded by hand — GnuCOBOL's `+00012.50` against WiseOwl's `000125{` — is folded by the harness:
`display_image()` reduces both display forms to sign and digits, two outputs equal under it are `OUTPUT_LATITUDE` under their
own heading and never a survivor, and a row that differs elsewhere lists its real differing lines first. The branch review
(round 1) showed the text fold cannot tell that latitude from an edited item whose point WiseOwl dropped, so the fold is
gated by the program's source: it applies only when no picture carries a point or an editing sign and no item is SIGN
SEPARATE. Replayed over the first night's results the gated fold takes 502 of 1,141 output rows and leaves 639 to read (174
fold-equal rows stay for the triage because their program declares an edited picture); the stored report was re-rendered
and the one the triage read kept beside it. The verdict names an open owner decision (PB3467) and settles nothing: the rows
are kept, only sorted.
