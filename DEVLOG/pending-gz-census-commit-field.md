## The three compressed member indexes name their new commit (kb/Work PB4814, review lead R2-1)

The history rewrite's follow-up renamed the arch-census evidence after each commit's new SHA, but the three
`*.members.json.gz` files still carried the old SHA in their `commit` field, because the remap rewrote text files and not gzip
contents. Each field now names the new commit, which is the file's own name (cd85e4901794, 6258c350c2e3, 8ae2228e86de), so
`member_index.py` reports a commit that exists on the rewritten remote.
