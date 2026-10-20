#!/usr/bin/env python3
"""Install the pinned Linux x64 SDK using the official release SHA-512."""
from pathlib import Path
import hashlib, subprocess, tarfile, tempfile, urllib.request
VERSION = '10.0.402'
URL = 'https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.402/dotnet-sdk-10.0.402-linux-x64.tar.gz'
SHA512 = '1904a98dda2db34ba78ba3c4fb1b224455329af91c341d83547843c1839e112b04ae699ee92fa2449a8f62249196a938723078c0c60b9f65c88539d5687729c2'
root = Path('/workspace/toolchains/dotnet')
if (root/'dotnet').exists():
    result = subprocess.run([str(root/'dotnet'), '--list-sdks'], check=True, capture_output=True, text=True)
    if any(line.startswith(VERSION + ' ') for line in result.stdout.splitlines()):
        print('Pinned SDK already installed:', VERSION)
        raise SystemExit(0)
root.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory() as temporary:
    archive = Path(temporary)/'sdk.tar.gz'
    urllib.request.urlretrieve(URL, archive)
    with archive.open('rb') as stream:
        actual = hashlib.file_digest(stream, 'sha512').hexdigest()
    if actual.lower() != SHA512.lower():
        raise RuntimeError('SDK SHA-512 mismatch; archive will not be used')
    with tarfile.open(archive) as files:
        files.extractall(root, filter='data')
print('Verified and installed SDK', VERSION)
