#!/usr/bin/env python3
"""Expose TRX failures as GitHub annotations so Windows failures remain inspectable."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
namespace={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
failed=False
for pattern in sys.argv[1:]:
    for file in Path(pattern).rglob('*.trx') if Path(pattern).is_dir() else [Path(pattern)]:
        if not file.exists():
            continue
        tree=ET.parse(file)
        for result in tree.findall('.//t:UnitTestResult',namespace):
            if result.get('outcome') not in ('Failed','Error','Timeout'):
                continue
            failed=True
            message=result.findtext('.//t:Message','No failure message',namespace)
            escaped=message.replace('%','%25').replace('\r','%0D').replace('\n','%0A')
            title=result.get('testName','Test failure').replace(',',' ').replace('\n',' ')
            print(f'::error title={title}::{escaped}',flush=True)
            print(result.get('testName'),message,flush=True)
raise SystemExit(1 if failed else 0)
