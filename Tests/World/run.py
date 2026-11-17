#!/usr/bin/env python3
"""Exercise world travel and geometry in an isolated copy of the complete gameplay scene."""
from pathlib import Path
import json, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
project=Path(tempfile.mkdtemp(prefix='CyclomaniaWorld-'))
for folder in ['Assets/Editor','Packages','ProjectSettings']:
    (project/folder).mkdir(parents=True)
shutil.copytree(root/'Assets',project/'Assets',dirs_exist_ok=True)
for name in ['ProjectVersion.txt','InputManager.asset']:
    shutil.copy2(root/'ProjectSettings'/name,project/'ProjectSettings')
shutil.copy2(Path(__file__).with_name('AuthoredLevelValidation.cs'),project/'Assets/Editor')
deps={k:v for k,v in json.loads((root/'Packages/manifest.json').read_text())['dependencies'].items() if k.startswith('com.unity.modules.')}
for package in (root/'Library/PackageCache').iterdir():
    manifest=package/'package.json'
    if manifest.exists():
        deps[json.loads(manifest.read_text())['name']]='file:'+str(package)
(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}))
print(project,flush=True)
result=subprocess.run(['/Applications/Unity/Hub/Editor/6000.0.79f1/Unity.app/Contents/MacOS/Unity','-batchmode','-nographics','--burst-disable-compilation','-projectPath',str(project),'-executeMethod','AuthoredLevelValidation.Run','-logFile',str(project/'validation.log')],timeout=600)
output=(project/'validation.log').read_text(errors='replace')
for line in output.splitlines():
    if any(marker in line for marker in ['LEVEL_CHECK_FAILED:','Exception:','error CS','AUTHORED_LEVEL_']):print(line)
raise SystemExit(result.returncode or (0 if 'AUTHORED_LEVEL_VALIDATION_SUCCESS' in output else 1))
