#!/usr/bin/env python3
"""Convert the pinned Scrollmapper ASV source JSON without supplying missing verses."""
import hashlib
import json
from pathlib import Path
import sys
source = Path(sys.argv[1]).read_bytes()
if hashlib.sha256(source).hexdigest() != 'e0c22535af3f65139408c5ea0a448e8a1a5d5123b79915090cedd8e41896b439':
    raise ValueError('Source differs from reviewed ASV dataset')
data = json.loads(source)
verses = [{'Book': book['name'], 'Chapter': chapter['chapter'], 'Number': verse['verse'], 'Text': verse['text'].strip()}
          for book in data['books'] for chapter in book['chapters'] for verse in chapter['verses'] if verse['text'].strip()]
assert len(data['books']) == 66 and len(verses) == 31086
assert len({(v['Book'],v['Chapter'],v['Number']) for v in verses}) == len(verses)
result = {'Title': 'American Standard Version (1901)',
          'Attribution': 'Public domain. Digital source: Scrollmapper.', 'Verses': verses}
output = Path(__file__).resolve().parent.parent/'src/Kworship.Desktop/Content/Bibles/asv.bible.json'
output.write_text(json.dumps(result,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
print(f'Converted {len(verses)} nonempty verses; 16 empty source entries omitted without renumbering.')
