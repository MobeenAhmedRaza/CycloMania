#!/usr/bin/env python3
"""Validate mission placement and handbrake in an isolated copy of the current map."""
from pathlib import Path
import json, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
project=Path(tempfile.mkdtemp(prefix='CyclomaniaLocations-'))
shutil.copytree(root/'Assets',project/'Assets')
(project/'Assets/Editor').mkdir(exist_ok=True)
(project/'Packages').mkdir()
(project/'ProjectSettings').mkdir()
for name in ['ProjectVersion.txt','InputManager.asset']:
    shutil.copy2(root/'ProjectSettings'/name,project/'ProjectSettings'/name)
shutil.copy2(Path(__file__).with_name('LocationSceneValidation.cs'),project/'Assets/Editor')
deps={k:v for k,v in json.loads((root/'Packages/manifest.json').read_text())['dependencies'].items() if k.startswith('com.unity.modules.')}
for name in ['com.unity.animation.rigging','com.unity.burst','com.unity.mathematics','com.unity.test-framework','com.unity.ext.nunit','com.unity.ugui','com.unity.splines','com.unity.collections']:
    package=next((root/'Library/PackageCache').glob(name+'@*'))
    deps[name]='file:'+str(package)
(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}))
print(project,flush=True)
log=project/'validation.log'
result=subprocess.run(['/Applications/Unity/Hub/Editor/6000.0.79f1/Unity.app/Contents/MacOS/Unity','-batchmode','-nographics','--burst-disable-compilation','-projectPath',str(project),'-executeMethod','LocationSceneValidation.Run','-logFile',str(log)],timeout=600)
output=log.read_text(errors='replace')
for line in output.splitlines():
    if any(marker in line for marker in ['Exception:','error CS','LOCATION_PLACEMENT_SUCCESS','LOCATION_SCENE_VALIDATION_SUCCESS']):print(line)
raise SystemExit(result.returncode or (0 if 'LOCATION_SCENE_VALIDATION_SUCCESS' in output else 1))
