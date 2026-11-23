**2026-11-23, wave 20262154 group E: one observable change besides the golden's is a recorded design consequence, not a defect.**

An ORDER TABLE literal-9 naming a POSIX codeset (`"es_ES.UTF-8"`) is now `EC-ORDER-NOT-SUPPORTED`. Before the wave it selected Spanish
by accident: the POSIX spelling fell through to `es.tailor`. DESIGN-locale-facility §4.9 says literal-9 is "a CLDR locale tag", and
`LocaleTag` now answers the spelling one way for every reader, so the refusal follows the design. The spellings `es_ES` and `es-ES`
(no codeset) stay accepted. Accepting the POSIX-codeset spelling would change §4.9 first, as a decision note; none is filed because
nothing asks for it.

The other lead from the same report, the .tailor chain versus the CLDR chain, is a different mechanism: kb/Work PB6137.
