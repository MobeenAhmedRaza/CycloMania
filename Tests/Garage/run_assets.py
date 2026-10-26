#!/usr/bin/env python3
"""Exercise the actual bicycle prefabs in an isolated Unity project (no player saves)."""
from pathlib import Path
import json, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
project=Path(tempfile.mkdtemp(prefix='CyclomaniaGarageAssets-'))
for folder in ['Assets/Editor','Packages','ProjectSettings']:
    (project/folder).mkdir(parents=True)
for name in ['Simple Bicycle Physics','Cyclomania']:
    shutil.copytree(root/'Assets'/name,project/'Assets'/name)
for name in ['ProjectVersion.txt','InputManager.asset']:
    shutil.copy2(root/'ProjectSettings'/name,project/'ProjectSettings')
shutil.copy2(Path(__file__).with_name('AssetValidation.cs'),project/'Assets/Editor')
deps={k:v for k,v in json.loads((root/'Packages/manifest.json').read_text())['dependencies'].items() if k.startswith('com.unity.modules.')}
for name in ['com.unity.animation.rigging','com.unity.burst','com.unity.mathematics','com.unity.test-framework','com.unity.ext.nunit','com.unity.ugui']:
    package=next((root/'Library/PackageCache').glob(name+'@*'))
    deps[name]='file:'+str(package)
(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}))
print(project,flush=True)
result=subprocess.run(['/Applications/Unity/Hub/Editor/6000.0.79f1/Unity.app/Contents/MacOS/Unity','-batchmode','-nographics','--burst-disable-compilation','-projectPath',str(project),'-executeMethod','AssetValidation.Run','-logFile',str(project/'validation.log')],timeout=600)
output=(project/'validation.log').read_text(errors='replace')
for line in output.splitlines():
    if any(marker in line for marker in ['PASS:','Exception:','error CS','GARAGE_ASSET_']):print(line)
raise SystemExit(result.returncode or (0 if 'GARAGE_ASSET_VALIDATION_SUCCESS' in output else 1))
