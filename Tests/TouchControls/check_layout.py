from pathlib import Path
import re
import itertools

root = Path(__file__).resolve().parents[2]
s = (root / 'Assets/Scenes/gameplay.unity').read_text()
blocks = {int(re.search(r'&([0-9]+)', b)[1]): b for b in re.split(r'(?=^--- !u!)', s, flags=re.M)[1:]}
ids = re.findall(r'^--- !u!\d+ &(\d+)', s, re.M)
assert len(ids) == len(set(ids))
names = {'Left', 'Right', 'Forward', 'Backward', 'Sprint', 'BHop', 'Wheelie', 'Reset'}
buttons = {}
for b in blocks.values():
    if not b.startswith('--- !u!1 '):
        continue
    m = re.search(r'^  m_Name: (.+)$', b, re.M)
    if not m or m[1] not in names:
        continue
    comps = list(map(int, re.findall(r'component: \{fileID: (\d+)\}', b)))
    rect = next(blocks[c] for c in comps if blocks[c].startswith('--- !u!224 '))
    def vec(k):
        return tuple(map(float, re.search(k + r': \{x: ([\d.-]+), y: ([\d.-]+)\}', rect).groups()))
    buttons[m[1]] = (vec('m_AnchorMin'), vec('m_AnchoredPosition'), vec('m_SizeDelta'))
assert len(buttons) == 8
for w, h, left, right, bottom in [(1334,750,0,0,0), (2556,1179,177,0,63), (2556,1179,0,177,63), (2360,1640,0,0,40), (1920,1080,0,0,0)]:
    scale = min(w/1920, h/1080)
    sw, sh = (w-left-right)/scale, (h-bottom)/scale
    rects = {}
    for name, (anchor, pos, size) in buttons.items():
        x, y = anchor[0]*sw+pos[0], anchor[1]*sh+pos[1]
        bw, bh = size
        assert x-bw/2 >= 0 and x+bw/2 <= sw and y-bh/2 >= 0 and y+bh/2 <= sh, name
        rects[name] = (x-bw/2, y-bh/2, x+bw/2, y+bh/2)
    for (an, a), (bn, b) in itertools.combinations(rects.items(), 2):
        assert a[2] <= b[0] or b[2] <= a[0] or a[3] <= b[1] or b[3] <= a[1], (an, bn)
assert blocks[430065872].count('propertyPath: mobileControls.') == 7
assert 'm_Target: {fileID: 2100000011}' in s and 'm_MethodName: Respawn' in s
print('PASS: 40 safe-area bounds checks, 140 non-overlap checks, 7 direct input bindings, reset callback, and unique scene IDs.')
