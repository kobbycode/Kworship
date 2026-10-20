#!/usr/bin/env python3
"""Promote verified Windows CI output to the existing development-release draft."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile


def gh(*arguments):
    return subprocess.check_output(['gh', *arguments], text=True)


def main():
    repository = os.environ['GITHUB_REPOSITORY']
    run_id = os.environ['BUILD_RUN_ID']
    if not re.fullmatch(r'[0-9]+', run_id):
        raise ValueError('Build run ID must be numeric')
    run = json.loads(gh('api', f'repos/{repository}/actions/runs/{run_id}'))
    if not (run['status'] == 'completed' and run['conclusion'] == 'success'
            and run['event'] == 'push' and run['head_branch'] == 'master'
            and run['path'] == '.github/workflows/build.yml'
            and run['head_repository']['full_name'] == repository):
        raise RuntimeError('Only successful master build runs from this repository can be promoted')
    commit = run['head_sha']
    release = json.loads(gh('release', 'view', 'v0.2.0', '--repo', repository,
                            '--json', 'isDraft,isPrerelease,targetCommitish'))
    if not release['isDraft'] or not release['isPrerelease'] or release['targetCommitish'] != commit:
        raise RuntimeError('Expected an unpublished development draft for the exact tested commit')

    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        # Check actual test counters rather than relying only on a green workflow.
        results = {}
        for operating_system in ('ubuntu-latest', 'windows-latest'):
            directory = root/operating_system
            subprocess.run(['gh', 'run', 'download', run_id, '--repo', repository,
                            '--name', 'test-results-'+operating_system, '--dir', str(directory)], check=True)
            files = list(directory.rglob('*.trx'))
            if len(files) != 1:
                raise RuntimeError('Expected exactly one test report for '+operating_system)
            tree = ET.parse(files[0])
            counters = tree.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters')
            if counters is None:
                raise RuntimeError('Missing test counters')
            passed, total, failed = (int(counters.get(name, '0')) for name in ('passed', 'total', 'failed'))
            if passed < 1 or passed != total or failed != 0:
                raise RuntimeError('Tests did not all pass on '+operating_system)
            results[operating_system] = passed
        package = root/'package'
        subprocess.run(['gh', 'run', 'download', run_id, '--repo', repository,
                        '--name', 'Kworship-0.2.0-win-x64', '--dir', str(package)], check=True)
        archive = package/'Kworship-0.2.0-win-x64.zip'
        receipt = package/'Kworship-0.2.0-win-x64.zip.sha256'
        expected = receipt.read_text().split()[0]
        with archive.open('rb') as stream:
            if hashlib.file_digest(stream, 'sha256').hexdigest() != expected:
                raise RuntimeError('Windows package checksum mismatch')
        with zipfile.ZipFile(archive) as zipped:
            if zipped.testzip() is not None:
                raise RuntimeError('ZIP integrity failure')
            prefix = 'Kworship-0.2.0-win-x64/'
            for name in ('Kworship.exe', 'coreclr.dll', 'e_sqlite3.dll', 'QUICKSTART.txt'):
                zipped.getinfo(prefix+name)
            with zipped.open(prefix+'Kworship.exe') as executable:
                if executable.read(2) != b'MZ':
                    raise RuntimeError('Expected Windows executable')
        notes = root/'release-notes.md'
        notes.write_text(f'''Kworship 0.2.0 is an early Windows x64 development build.

Download **Kworship-0.2.0-win-x64.zip**, right-click it in Windows and choose **Extract All**, then open the extracted folder and run **Kworship.exe**. Keep all extracted files together. The .NET runtime is included.

Includes song editing/search, saved service schedules, independent preview/live controls, congregation/stage text windows, and offered draft recovery. Take a preview and toggle Black off to reveal content. F11 toggles output-window fullscreen.

This ZIP was produced by Windows CI for commit `{commit}`. All {results['windows-latest']} tests passed on Windows and all {results['ubuntu-latest']} passed on Linux. Its SHA-256 and ZIP integrity were verified before publication.

[Build and test results]({run['html_url']})

This is an unsigned development release. Interactive UI, physical displays and live-service reliability still require manual validation. Media playback, scripture, PowerPoint, automatic display assignment and streaming are not implemented. No song or Bible corpus is bundled.

See QUICKSTART.txt and docs/14-operator-workspace.md for details. The SHA-256 receipt is included to check your downloaded ZIP.
''')
        # The release stays a draft until both verified assets have uploaded.
        subprocess.run(['gh', 'release', 'upload', 'v0.2.0', str(archive), str(receipt),
                        '--repo', repository, '--clobber'], check=True)
        subprocess.run(['gh', 'release', 'edit', 'v0.2.0', '--repo', repository,
                        '--title', 'Kworship 0.2.0 — Development preview', '--notes-file', str(notes),
                        '--draft=false', '--prerelease'], check=True)
        print(gh('release', 'view', 'v0.2.0', '--repo', repository, '--json', 'url', '--jq', '.url').strip())
        print('Verified test counts:', results)


if __name__ == '__main__':
    main()
