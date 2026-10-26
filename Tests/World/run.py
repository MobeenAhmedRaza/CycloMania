#!/usr/bin/env python3
"""Exercise world travel and geometry in an isolated copy of the complete gameplay scene."""
from pathlib import Path
import json, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
project=Path(tempfile.mkdtemp(prefix='CyclomaniaWorld-'))
for folder in ['Assets/Editor','Packages','ProjectSettings']:
    (project/folder).mkdir(parents=True)
for name in ['Simple Bicycle Physics','Cyclomania','MobileCity','BikeMania_GUI','Scenes']:
    shutil.copytree(root/'Assets'/name,project/'Assets'/name)
for name in ['ProjectVersion.txt','InputManager.asset']:
    shutil.copy2(root/'ProjectSettings'/name,project/'ProjectSettings')
shutil.copy2(Path(__file__).with_name('WorldSceneValidation.cs'),project/'Assets/Editor')
deps={k:v for k,v in json.loads((root/'Packages/manifest.json').read_text())['dependencies'].items() if k.startswith('com.unity.modules.')}
for name in ['com.unity.animation.rigging','com.unity.burst','com.unity.mathematics','com.unity.test-framework','com.unity.ext.nunit','com.unity.ugui']:
    package=next((root/'Library/PackageCache').glob(name+'@*'))
    deps[name]='file:'+str(package)
(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}))
print(project,flush=True)
result=subprocess.run(['/Applications/Unity/Hub/Editor/6000.0.79f1/Unity.app/Contents/MacOS/Unity','-batchmode','-nographics','--burst-disable-compilation','-projectPath',str(project),'-executeMethod','WorldSceneValidation.Run','-logFile',str(project/'validation.log')],timeout=600)
output=(project/'validation.log').read_text(errors='replace')
for line in output.splitlines():
    if any(marker in line for marker in ['PASS:','Exception:','error CS','WORLD_SCENE_']):print(line)
raise SystemExit(result.returncode or (0 if 'WORLD_SCENE_VALIDATION_SUCCESS' in output else 1))
