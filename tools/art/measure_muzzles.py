"""Locate the gun barrel tip in every hero frame of Assets/ArtSources/heroes.png (the old 4 x 4 sheet) and print the "muzzle" anchors,
in world units from the actor's feet, to paste in the "animations" of Resources/Content/heroes/*.json.
The unit conversion mirrors the heroes' sheet settings (cell crop, pivot and cellHeight 2.2777777).
usage: measure_muzzles.py [--preview OUT.png]   (needs Pillow)"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ART = Path(__file__).resolve().parents[2] / 'Assets/ArtSources'
COLUMNS = 4


def to_world(px, py, w, h):
    ppu = h * .9 / 2.05
    return (px - w * .4) / ppu, ((h - py) - .045 * h) / ppu


def barrel_tip(cell):
    """Rightmost opaque pixel of the gun band; the barrel is the forward-most thing at chest height."""
    w, h = cell.size
    alpha = cell.getchannel('A').load()
    top, bottom = int(h * .2), int(h * .55)
    tip = max((x for x in range(w) for y in range(top, bottom) if alpha[x, y] > 128), default=None)
    rows = [y for y in range(top, bottom) for x in range(max(0, tip - 5), tip + 1) if alpha[x, y] > 128]
    return tip, sum(rows) / len(rows)


def main():
    sheet = Image.open(ART / 'heroes.png').convert('RGBA')
    rows = 4
    w, h = sheet.width / COLUMNS, sheet.height / rows
    preview = sheet.copy()
    draw = ImageDraw.Draw(preview)
    lines = []
    for row in range(rows):
        for col in range(COLUMNS):
            cell = sheet.crop((round(col * w), round(row * h), round((col + 1) * w), round((row + 1) * h)))
            px, py = barrel_tip(cell)
            x, y = to_world(px, py, w, h)
            lines.append(f'{{"name": "muzzle", "cell": {row * COLUMNS + col}, "x": {x:.2f}, "y": {y:.2f}}}')
            cx, cy = col * w + px, row * h + py
            draw.ellipse((cx - 6, cy - 6, cx + 6, cy + 6), outline=(255, 0, 255, 255), width=3)
    print(',\n'.join(lines))
    if '--preview' in sys.argv:
        preview.save(sys.argv[sys.argv.index('--preview') + 1])


if __name__ == '__main__':
    main()
