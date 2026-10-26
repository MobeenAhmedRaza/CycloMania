#!/usr/bin/env python3
"""Run core physics checks in a disposable Unity project, leaving the game/editor untouched."""
import argparse
from pathlib import Path
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument('--unity', default='/Applications/Unity/Hub/Editor/6000.0.79f1/Unity.app/Contents/MacOS/Unity')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
project = Path(tempfile.mkdtemp(prefix='CyclomaniaCoreValidation-'))
for directory in ['Assets/Editor', 'Assets/Plugins', 'ProjectSettings', 'Packages']:
    (project / directory).mkdir(parents=True)
for name in ['ProjectVersion.txt', 'InputManager.asset']:
    shutil.copy2(root / 'ProjectSettings' / name, project / 'ProjectSettings')
shutil.copy2(root / 'Library/ScriptAssemblies/UnityEngine.UI.dll', project / 'Assets/Plugins')
for name in ['BicycleController.cs', 'BicycleStatus.cs', 'MobileButtonHandler.cs', 'BicycleCamera.cs']:
    shutil.copy2(root / 'Assets/Simple Bicycle Physics/Scripts' / name, project / 'Assets')
for name in ['CycleRecovery.cs', 'CycleImpactRelay.cs', 'TouchControlGraphic.cs', 'GameplayHud.cs', 'RideSession.cs', 'RideMinimap.cs', 'HudShape.cs', 'SafeAreaPanel.cs', 'StuntScoring.cs', 'BicycleStunts.cs', 'GarageProgress.cs', 'GameplayGarage.cs', 'RideWorld.cs', 'MissionSession.cs', 'MissionCareer.cs', 'RideMissions.cs', 'GameplayMissions.cs', 'FreestyleCatalog.cs', 'GameplayFreestyle.cs', 'GameEconomy.cs', 'GameplayEconomy.cs', 'RecurringProgress.cs', 'GameplayRecurring.cs', 'AdvancedProgress.cs', 'GameplayAdvanced.cs', 'RideEnvironment.cs', 'RideReplay.cs', 'GamePreferences.cs', 'GameSettings.cs', 'SettingsAudioChannel.cs']:
    shutil.copy2(root / 'Assets/Cyclomania/Runtime' / name, project / 'Assets')
for test in Path(__file__).parent.glob('*.cs'):
    shutil.copy2(test, project / 'Assets/Editor')
(project / 'Packages/manifest.json').write_text('{"dependencies":{"com.unity.modules.particlesystem":"1.0.0","com.unity.modules.physics":"1.0.0","com.unity.modules.ui":"1.0.0","com.unity.modules.imgui":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.jsonserialize":"1.0.0"}}\n')
log = project / 'validation.log'
print(f'Test project and log: {project}', flush=True)
result = subprocess.run([args.unity, '-batchmode', '-nographics', '-projectPath', str(project),
                         '-executeMethod', 'CoreValidation.Run', '-logFile', str(log)], timeout=180)
output = log.read_text(errors='replace') if log.exists() else ''
for line in output.splitlines():
    if any(marker in line for marker in ['PASS:', 'Exception:', 'error CS', 'CORE_VALIDATION_', 'TOUCH_VALIDATION_', 'HUD_VALIDATION_', 'STUNT_VALIDATION_', 'GARAGE_VALIDATION_', 'MISSION_VALIDATION_', 'POLICE_VALIDATION_', 'CANYON_VALIDATION_', 'HANDLING_VALIDATION_', 'FREESTYLE_VALIDATION_', 'SETTINGS_VALIDATION_', 'ECONOMY_VALIDATION_', 'ADVANCED_VALIDATION_', 'RECURRING_VALIDATION_']):
        print(line)
raise SystemExit(result.returncode if result.returncode else (0 if 'CORE_VALIDATION_SUCCESS' in output and 'TOUCH_VALIDATION_SUCCESS' in output and 'HUD_VALIDATION_SUCCESS' in output and 'STUNT_VALIDATION_SUCCESS' in output and 'GARAGE_VALIDATION_SUCCESS' in output and 'MISSION_VALIDATION_SUCCESS' in output and 'POLICE_VALIDATION_SUCCESS' in output and 'CANYON_VALIDATION_SUCCESS' in output and 'HANDLING_VALIDATION_SUCCESS' in output and 'FREESTYLE_VALIDATION_SUCCESS' in output and 'SETTINGS_VALIDATION_SUCCESS' in output and 'ECONOMY_VALIDATION_SUCCESS' in output and 'ADVANCED_VALIDATION_SUCCESS' in output and 'RECURRING_VALIDATION_SUCCESS' in output else 1))
