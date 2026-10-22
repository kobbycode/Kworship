# Bulk hymn import (0.5.0)

## Use

1. Click **Import hymns…** in the content library.
2. **Choose hymn file…** accepts a UTF-8 CSV or JSON file (up to 20 MB / 2,000 hymns).
3. Review the hymn list, lyrics, credits and duplicate counts. Invalid files are rejected before anything is saved.
4. Click **Import collection**. All new records are committed together. Existing songs are never overwritten, including songs with the same title but different lyrics or credits.
5. Select an imported hymn from the main library, preview it, add it to your service, and save the service. Take it live explicitly; import never changes live output, preview or existing editor drafts.

The dialog can **Save example JSON…**. The distribution includes `hymn-example.json` with original demonstration words, not a historical hymn collection. Modify it to contain lyrics you are authorised to project. There is no automatic download, scraping, SongSelect connection or complete hymnal in this release.

## JSON

The root is an array. Property names are case-insensitive. Required fields are `title` and `lyrics`; optional strings are `author`, `copyright`, `arrangement`, `hymnal`, `number`. Unknown fields, null fields, empty titles/lyrics, invalid play orders and oversized values are rejected. Numbers must be strings, so leading zeros can be preserved.

```json
[
  {
    "title": "Original example hymn",
    "lyrics": "[Verse 1]\nOriginal example verse\n\n[Chorus]\nOriginal example chorus",
    "author": "Your author attribution",
    "copyright": "Your required projection notice",
    "arrangement": "Verse 1, Chorus, Verse 1",
    "hymnal": "Example hymnal",
    "number": "12"
  }
]
```

`hymnal` and `number` must both be supplied or both omitted. They appear in the saved title, e.g. `Example hymnal #12 — Original example hymn`. This makes hymn numbers searchable without a database migration. It is an operator-supplied reference, not a verified numbering catalogue. A label such as `SDA 1985` does not indicate inclusion or approval of the full hymnal.

Use real newline characters in decoded JSON lyrics (`\n` in the file) and blank lines between slides. Bracket labels such as `[Verse 1]` are section labels. Optional arrangement references those labels, separated by commas, including repeats. Without an arrangement, sections appear in source order. Each imported hymn is a normal editable local song.

## CSV / spreadsheets

The header requires `title,lyrics`; other columns are optional and use the JSON field names. Order is flexible. Duplicate or unknown column names are rejected. Use UTF-8 (a BOM is accepted). Quote fields containing commas, quotes or newlines. Double an embedded quote (`""`). Put actual line breaks inside quoted lyrics; literal backslash-n is not converted in CSV.

```csv
title,lyrics,author,copyright,hymnal,number
"Original example","[Verse 1]
Original example verse

[Chorus]
Original example chorus","Example writer","Original demonstration words","Example hymnal","12"
```

## Duplicates, failures and backup

“Exact duplicates” compare title, lyrics, author, copyright and arrangement after normalising line endings, outer whitespace and case. Both existing-library and within-file duplicates are skipped. Different wording, notices or play orders are retained as separate songs. All-duplicate files cannot be confirmed in the dialog. Duplicate decisions are rechecked inside the actual write transaction.

An insertion failure rolls back the entire new batch. Imports do not overwrite existing IDs/revisions, update saved service references or replace unsaved drafts. Imported hymns and their credits are included in **Back up library** after the import finishes. They are not automatically added to a service.

Keep source files and licensing records. CSV/JSON support here is Kworship's documented schema, not a claim of compatibility with arbitrary hymnbook exports. PDF, ZIP, HTML and proprietary databases are not supported.

## SDA hymnal coverage and reuse status

On 2026-10-22, `https://sdahymnals.com/Hymnal/` returned HTTP 403 from this environment, including an access retry. Its contents, completeness, rights-holder information and reuse terms could not be inspected. No full collection was copied from that site and no reuse permission is claimed. Web availability alone is not a redistribution licence. Historic hymn texts, modern revisions, tunes/arrangements and hymnal editions may have different rights.

The user-uploaded `Kworship-master.zip` matched source commit `8cc5e05` (0.4.1) exactly. It contained no hymn-import implementation, ten-hymn collection, or 131-test suite. This release implements import from that verified baseline; it does not claim to restore those missing changes or the full SDA hymnal.

## Validation

Core tests cover JSON metadata/numbering/order, CSV multiline lyrics and quoted fields, malformed or oversized files, duplicate decisions, transactional rollback and preserving live content/drafts/services. Windows UI smoke exercises the real import dialog, rejects an invalid file, reviews/imports a collection with duplicates, previews its repeated arrangement, saves an imported hymn alongside a song and scripture, and disables an all-duplicate reimport. File selection uses an internal path entry in smoke; the native file-picker and physical church projection still require manual rehearsal.
