**2026-11-12: GnuCOBOL closed bug 1286 as fixed in r5775.**

On 2026-11-12 at 12:58 UTC the maintainer Simon Sobisch (sf-mensch) closed ticket 1286 (https://sourceforge.net/p/gnucobol/bugs/1286/) with the comment "Fix was checked in with [r5775]." (read from the SourceForge REST API that day and cached at E:/COBOL-coord/scratch/sf-tickets/bugs-1286.json).

The fix is the attached one-condition patch: the "no definition/prototype seen" warning now tests the -Wprototypes option. The report itself called the question of diagnosing §2023:12.3.8.3 SR14 without a warning option "a separate question from this bug", and nothing in the ticket says r5775 changed that.

In the same comment he asked for the FSF copyright assignment before other patches can be applied. That is the owner's to act on.

The note's frontmatter is now `status: retired` and `upstream_status: fixed-upstream`, as in kb/Work PB3899 and PB4538.
