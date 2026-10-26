## PB3682: a TYPE declaration takes the usage and SIGN walks, and a description copied before them still has its subordinates

PB3682 filed one silent screen: a TYPEDEF `GROUP-USAGE NATIONAL` over a `GROUP-USAGE BIT` subgroup compiled while its storage twin drew
COBOLNET1653. Re-probed on the branch build, the root is the walk, not the block: `UsageInheritancePass` walked `Roots` only, so none of
the §13.18.60.4 GR1 screens reached a TYPE declaration (a TYPEDEF group `USAGE BINARY` over a `USAGE PACKED-DECIMAL` member, over a `PIC X`
member, `USAGE PACKED-DECIMAL WITH NO SIGN` over `PIC S9`, a VALUE under `USAGE INDEX` were all silent unreferenced). Beside it ran the opposite
hole: a valid `01 TG TYPEDEF USAGE BINARY-LONG. 05 A. 05 B.` was refused (COBOLNET2191 and COBOLNET0881), and probing that shape found three more
faults with one cause.

Fixes. (1) `UsageInheritancePass` and `InheritSignClauses` walk `AllTypeDecls()` after the roots, so a declaration answers for itself and its
members take their group's usage and SIGN as a TYPE subject's do. That makes a template and its subject one description to the §8.5.3.1
comparison: `SET P TO ADDRESS OF W` with P restricted to a STRONG type whose group wrote USAGE or SIGN was refused COBOLNET0869 and is accepted.
A copy takes the walk's application but not its screens where the fact is its declaration's (`AnsweredByTypeDeclaration`, through the new
`EditionContext.Muted`), so a TYPEDEF referenced twice draws one diagnostic naming the declaration, not one per reference naming the subject;
a clause from outside the copy and §13.18.29.3 SR1's "not strongly typed" stay the copy's. A copy of a template the container's bind already
walked (a GLOBAL declaration in a contained program) arrives with its leaves' profiles, and the elementary arm leaves them alone. (2) The
TYPE and SAME AS copies run before the walk and asked `IsGroup` (`Pic is null`), false of `01 G USAGE BINARY-LONG. 05 A.` until the walk
sheds the header's profile: `01 V SAME AS X` over such a group was ONE elementary item a quarter of X's size (BYTE-LENGTH 8 against 4, a silent
wrong answer), a TYPE subject got no members (`A OF W` undefined), `TYPEDEF STRONG` of the shape drew COBOLNET1529, and `SAME AS H` of a level-5
group with a usage passed SR7. They ask `DataItem.HasSubordinates` now, pinned by a drift test. (3) `CompleteDescriptionAhead` recorded the
profile as written for a later copy but not its provenance or the deferred mark, so a SAME AS after `CONSTANT AS LENGTH OF X` read the header's
profile as a written PICTURE on a group; `PictureAsWritten` keeps all three.

Goldens: nine refused programs, one per screen on a TYPEDEF tree, plus the SAME AS level-1 refusal; five accepted programs (the template, the
GLOBAL template in a contained program, SAME AS, SAME AS after a length constant, the restricted pointer). `UsageInheritanceDriftTests` gains the
third spelling (`01 TG TYPEDEF USAGE u. 05 A pic.`, typed and unreferenced) over its whole usage x phrase x picture matrix, which fails 49 of 77
cells with the template walk removed, plus tests for once-per-declaration and for the copies' subordinates. The inventory rows were already
CONFORMS for their storage arm: witnesses added to 15 rows (`closes_rows: []` with its reason). Left as a lead: SAME AS of a picture-less LEAF under
a group-level picture-less usage is refused as not elementary (SR7 reads `Pic`, null until the walk). Self-review found the walked-template copy
hazard (a contained program copying a completed template) and the muted-diagnostic dedupe before the gate; both have witnesses above.
