## INVOKE NULL compiles and raises EC-OO-NULL; activation-description keys are culture-invariant (PB1136, PB3257)

Wave 1112 group B, the OO binder group: kb/Work PB1136's receiver half and PB3257.

**PB1136 — INVOKE NULL.** Re-probed on `e679ed0a9`: the object-view receiver (`INVOKE U AS C "M"`) and the
inline-invocation receiver (`INVOKE O :: "ME" "M"`) already worked (kb/Work PB1425); `INVOKE NULL "M"` was still the
deferral COBOLNET1756 and a run-time `NotImplementedCobolFeatureException`. ISO §14.9.23.3 SR1 asks only that
identifier-1 be an object reference, and §8.4.3.7.3 SR2 describes NULL as "class object and category object reference,
and is not a universal object reference": literal-1 is required (identifier-2 stays SR7's COBOLNET0866), no SR4 a)-i)
roster constrains it, SR6 does not apply (BY CONTENT and BY VALUE are legal), and SR5 / SR18 have no formal to compare
with. §14.9.23.4 GR5 is the meaning: EC-OO-NULL, statement terminated. `OoBinder.OoBindNullInvoke` asks the syntax rules
that read only the operand (`OoScreenNullReceiverArgument`: SR9/SR10 of an explicit BY REFERENCE identifier-3, a
keyword-less identifier BY REFERENCE only when it meets them per GR6 a), SR12, SR13, SR15, SR16, SR17;
`OoScreenReturning`: SR11-SR13) and binds `BoundInvokeNull`, which carries nothing; `OoEmitter.EmitNullInvoke` renders
the one null-receiver guard `CobolObject.RequireNonNull((CobolObject?)null)`, and `EcBinder.NodeKindRaises` gives the
node its EC-OO-NULL row so a USE declarative receives it. Golden `14.9.23-INVOKE/pb1136_invoke_null_receiver` (2002:
HANDLED=EC-OO-NULL twice, the RETURNING item and a BY REFERENCE argument unchanged); negatives
`pb1136-invoke-null-identifier-2` (SR7) and `pb1136-invoke-null-by-value-alphanumeric` (SR15). Rows SR-14.9.23.3-1 and
SR-8.4.3.7.3-2 re-verdicted CONFORMS (GAP 58 -> 56). Design: COBOLNET_OO_DESIGN D5.

**One rule, one code, on every receiver.** The sweep found SR15 and SR17 written once per lane (now
`ByValueClassAdmitted` and `ZeroLengthLiteralAdmitted`, shared), and SR16 and SR10 reported under the generic argument
code COBOLNET0828 on the typed lane and, for SR10, COBOLNET0866 on the universal one. They are now catalog descriptors
COBOLNET4268 (SR16) and COBOLNET4269 (SR10), reported by `ReportByValueLiteral` / `ReportObjectDataByReference` on the
typed, universal and NULL lanes alike; the two pins move with them (`pb1051-by-value-literal-alphanumeric`,
`OoSpineTests.ObjectData_AutoContent_Sr10`). The semgrep raw-code count stays 245.

**PB3257 — culture-invariant keys.** The unit witness `ActivationKeyCultureTests` (a clone of the invariant culture with
U+2212 as its negative sign, sv-SE's on .NET 10) failed on the unfixed code for all three keys of a `PIC 99P` item:
`StrongTypeModel.TypeIdentityKey` (the PicInfo record dump's negative Scale), `ActivationDescriptions.ElementaryClauses`
(`DIGITS {Digits}.{Scale}`) and a sibling the finding did not name, `PictureClauseIdentity.Key`, whose absent currency
string prints `-1` in every picture key. The keys are compiled into the program and compared ordinally with another
compilation's, so a type keyed apart from itself across hosts. The appends and interpolations now take
`CultureInfo.InvariantCulture`, and the record dump is rendered by `InvariantRecordText` (a record's `PrintMembers` takes
no provider, so the thread's culture is set for the call and restored), which keeps "every axis by construction". The
bind-time caller key and emit-time method key now agree whatever backend scope kb/Work PB3239 adds (addendum there).
`RedefinesModel.RenamesSpanPart.SubscriptsFor` no longer formats text (kb/Work PB2151), so that site needed nothing. No
row closed; the witness is recorded on GR-8.5.3.1-1 and SR-14.8.2.2-2.

Self-review: the four lenses plus pr-test-analyzer (the culture witness was run red on the unfixed code first),
silent-failure-hunter (every null-lane screen reports before it returns false) and comment-analyzer.

Branch review (round 1) found one comment defect, R1-1: the `OoBindNullInvoke` doc comment said identifier-1 is the
first operand §14.9.23.4 GR7 a) evaluates, so the node could only be the raise. GR7 a) evaluates arithmetic-expression-1,
boolean-expression-1 and identifier-1, -2, -3 and -5 together at the beginning of the statement, and the argument binding
hoists an argument's activations (an inline invocation, a user-defined function, an object view) ahead of it, so they run
before EC-OO-NULL is raised, which GR7 a) allows. The comment and `BoundInvokeNull`'s now state that order, and
pb1136_invoke_null_receiver gains an `O :: "GETV"` argument whose EVALUATED line, printed before HANDLED, pins it.
