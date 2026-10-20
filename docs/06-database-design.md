# Proposed database and storage design

SQLite proposal; no database created in Phase 1. Schema names below define intended keys and relationships, not executable migrations.

| Table | Key fields / relationships |
|---|---|
| schema_migrations | version PK, checksum, applied_utc |
| songs | id UUID PK, title, author, copyright, license_reference, language, favorite, revision, created_utc, updated_utc |
| song_aliases | id PK, song_id FK, alias |
| song_sections | id PK, song_id FK, label, body, ordinal |
| song_arrangements / arrangement_entries | arrangement id, song FK; entry id, arrangement FK, section FK, ordinal |
| tags / song_tags / categories / song_categories | normalized labels and unique joins |
| bible_translations | id PK, name, language, license_text, source, distribution_permission, content_version |
| bible_verses | translation FK, canonical_book, chapter, verse, body; composite PK |
| media_assets | id PK, relative_path, hash, size, detected_type, duration, rights_metadata, probe_status |
| themes | id PK, version, validated_definition_json, background_asset FK nullable |
| schedules | id PK, title, service_date, revision |
| schedule_items | id PK, schedule FK, ordinal, kind, content_ref, content_revision, overrides_json |
| preferences / output_profiles | stable key/value version; logical role, device identity, aspect/safe-area settings |
| presentation_history | id PK, item_id, committed_revision, taken_utc, outcome |
| song_revisions / recovery_drafts | immutable prior content; draft owner and last_saved_utc |

Use explicit per-kind reference tables or application validation for polymorphic schedule references; a bare content_ref cannot enforce referential integrity alone. Deleting a library item must first surface affected schedules and offer retained snapshots. Schedule ordinals are unique per schedule and reordered transactionally.

Enable foreign keys on every connection. Evaluate WAL with a serialized writer, bounded busy timeout and appropriate synchronous durability on local disk; never place a live WAL database on a network share. Use parameterized queries, immutable IDs, optimistic revision checks and transactions for edits/imports. FTS indexes cover song titles/aliases/lyrics and authorized verse text; rebuild indexes from canonical tables. Rights metadata must survive export.

Migrations are monotonic and checksummed. Before upgrade, create a verified backup; apply migration atomically where supported. On failure retain the original and show recovery options; never silently drop/recreate user data. Run integrity checks on suspect shutdown and provide diagnostic reports rather than automatic destructive repair.

Store imported media under an application-data asset root using content hashes and validated relative paths. Do not overwrite user originals. Backup is a versioned manifest plus a consistent SQLite online backup and referenced assets with hashes. Do not copy a live database file without SQLite's supported consistency mechanism. Restore to a staging directory, validate format, hashes, database integrity and media paths, then switch atomically with rollback copy. Report missing optional assets explicitly. Protect against archive traversal, decompression bombs and invalid schema versions.
