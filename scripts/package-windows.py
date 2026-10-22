#!/usr/bin/env python3
"""Package an already-published self-contained Windows build with its notices."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import zipfile

publish = Path(sys.argv[1]).resolve()
repository = Path(__file__).resolve().parent.parent
for name in ('Kworship.exe', 'Kworship.dll', 'coreclr.dll', 'e_sqlite3.dll', 'PresentationFramework.dll'):
    if not (publish/name).is_file():
        raise RuntimeError(f'Missing required self-contained output: {name}')
with (publish/'Kworship.exe').open('rb') as stream:
    if stream.read(2) != b'MZ':
        raise RuntimeError('Expected a Windows executable')
config = json.loads((publish/'Kworship.runtimeconfig.json').read_text())
frameworks = {item['name']:item['version'] for item in config['runtimeOptions']['includedFrameworks']}
packages = Path(os.environ.get('NUGET_PACKAGES',str(Path.home()/'.nuget'/'packages')))
notices = publish/'licenses'
notices.mkdir(exist_ok=True)
for source in (repository/'packaging'/'licenses').iterdir():
    shutil.copyfile(source,notices/source.name)
for package,framework in [('microsoft.netcore.app.runtime.win-x64','Microsoft.NETCore.App'),('microsoft.windowsdesktop.app.runtime.win-x64','Microsoft.WindowsDesktop.App')]:
    package_root = packages/package/frameworks[framework]
    found = False
    for name in ('LICENSE','LICENSE.TXT','THIRD-PARTY-NOTICES.TXT'):
        if (package_root/name).is_file():
            shutil.copyfile(package_root/name,notices/(package+'-'+name))
            found = True
    if not found:
        raise RuntimeError(f'Cannot package without runtime license: {package_root}')
for name in ('QUICKSTART.txt','THIRD-PARTY-NOTICES.txt','hymn-example.json'):
    shutil.copyfile(repository/'packaging'/name,publish/name)
shutil.copyfile(repository/'docs'/'18-hymn-import.md',publish/'HYMN-IMPORT.md')
archive = Path(str(publish)+'.zip')
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as output:
    for source in sorted(publish.rglob('*')):
        if source.is_file():
            output.write(source,Path(publish.name)/source.relative_to(publish))
with zipfile.ZipFile(archive) as output:
    bad = output.testzip()
    if bad:
        raise RuntimeError('ZIP verification failed: '+bad)
with archive.open('rb') as stream:
    digest = hashlib.file_digest(stream,'sha256').hexdigest()
archive.with_suffix('.zip.sha256').write_text(digest+'  '+archive.name+'\n')
print(f'Created and verified {archive} ({archive.stat().st_size//1024//1024} MiB)')
print('SHA-256:',digest)
