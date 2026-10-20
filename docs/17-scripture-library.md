# Scripture library (0.4.0)

## Operate

1. Open **Scripture library…** under the song-library controls.
2. Select **American Standard Version (1901)**, then book, chapter, first verse and last verse.
3. Review the passage. **Save passage to library** saves a new text item and selects it in the main library.
4. **Preview selected**, or **Add to service** and save the service. Scripture and hymns use the same service list. Each verse is one slide with its reference in the projected text; translation/source credits appear in the congregation footer.
5. Open congregation output, **Take**, then switch **Black** off. Live arrows advance verses. Saving/importing never takes content live.

Passages are independent saved copies: reimporting a translation does not change existing services or live snapshots. They currently appear as “Song” in the shared library/service list and can be edited with the song editor. They are not linked to their source translation after saving. Avoid duplicate saves unless you want separate editable copies.

## Included ASV and provenance

The public-domain **American Standard Version (1901)** is included offline. This does not include modern ASV revisions or other publishers' translations.

Source: Scrollmapper `bible_databases`, commit `d0ded4193d330a5412c14124a953bc112e67002f`:

- [ASV source JSON](https://github.com/scrollmapper/bible_databases/blob/d0ded4193d330a5412c14124a953bc112e67002f/sources/en/ASV/ASV.json)
- [Translation-specific licence statement](https://github.com/scrollmapper/bible_databases/blob/d0ded4193d330a5412c14124a953bc112e67002f/sources/en/ASV/README.md): **“License: Public Domain”**.
- Source JSON SHA-256: `e0c22535af3f65139408c5ea0a448e8a1a5d5123b79915090cedd8e41896b439`.
- The repository's MIT notice is also included as `licenses/scrollmapper-MIT.txt`, retrieved from master commit `e1b254cef86d0e65b1a5d1a94b8b112d0f296a2c`. The translation-specific public-domain declaration is the basis for including this text.

`scripts/convert-asv.py <source-json>` verifies that exact source hash, preserves source book names and verse numbers, trims leading/trailing whitespace, and creates the bundled JSON. No wording is supplied by Kworship. The resulting dataset has 66 books, 1,189 chapters and 31,086 nonempty verse entries. The source has 16 empty numbered entries: Matthew 17:21, 18:11, 23:14; Mark 7:16, 9:44, 9:46, 11:26, 15:28; Luke 17:36, 23:17; John 5:4; Acts 8:37, 15:34, 24:7, 28:29; Romans 16:24. These are omitted without renumbering. A range spanning a missing entry is rejected; select separate ranges around it. The bundled text should be checked against the church's reference edition before use. No claim of textual collation is made.

## Import another authorised translation

Obtain an uncompressed **Zefania XML** file from its publisher or authorised distributor. Enter its required attribution, licence and source in the dialog, then **Import Bible XML…**. Kworship does not acquire a licence on your behalf. OSIS, USFM, encrypted modules, PDFs and ZIP files are not supported by this importer.

Required structure: `XMLBIBLE` with `biblename`, `BIBLEBOOK` with unique `bname`, `CHAPTER cnumber`, and `VERS vnumber`. Positive chapter/verse numbers and nonempty text are required. XML DTDs/external entities are prohibited. Inline STYLE text is preserved; NOTE/XREF annotations are excluded. The import validates the whole document before atomically creating a new local file. Existing translations are never overwritten. Reimports are separate entries. Imported metadata is entered by the operator, rather than inferred as a licence from arbitrary XML fields.

Imports are stored at `%LOCALAPPDATA%\Kworship\Bibles\*.bible.json`. Keep the original XML and its licence documentation. **Back up library** covers saved passages and songs in SQLite, but does not include these source translation files or drafts. Copy the Bibles folder separately for a full source-library backup.

## Validation

Automated tests cover import persistence, source counts and representative ASV text, missing/invalid verse ranges, attribution, duplicate-address rejection, XML entity protection, projection references/credits, and passage/service reload while preserving live content and hymn drafts. Windows UI smoke opens the real scripture dialog, imports original fixture text, chooses a range, captures its preview, saves through the button, and saves the passage alongside a song in a service. It does not automate the Windows file-picker dialog itself. Physical projection and a church rehearsal remain manual checks.

Hymns can be entered with the existing song editor. Bulk hymn import, SongSelect integration and local image/video playback remain future milestones.
