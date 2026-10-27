#!/usr/bin/env python3
"""Repack a generated sprite sheet so every frame shares the game's pivot.

Each frame is cut from its grid cell (largest connected sprite), cleaned of faint alpha, then
placed in a new grid with its feet on a common baseline and the middle of its legs on the pivot
column: 40% of the cell for enemies, as their "sheet" settings in Resources/Content expect (pivot x .4,
y .045, cropLeft .065), the centre for the mecha. Prints the barrel tips to write as "muzzle" anchors.

usage: pack_sheet.py enemies IN.png OUT.png   (4 x 3 grid -> 4 x 3 cells of 420 x 362)
       pack_sheet.py mecha IN.png OUT.png     (3 x 2 grid -> 3 x 2 cells of 560 x 512)
Requires Pillow, NumPy and SciPy."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

LAYOUTS = {
    # cols, rows, cell w, cell h, feet margin (px), pivot column (fraction of cell width)
    "enemies": (4, 3, 420, 362, 16.29, .40),
    "mecha": (3, 2, 560, 512, 12.8, .50),
}


def frames(a, cols, rows):
    h, w = a.shape[:2]
    lab, _ = ndimage.label(a[..., 3] > 40)
    out = {}
    for i, sl in enumerate(ndimage.find_objects(lab)):
        mask = lab[sl] == i + 1
        if mask.sum() < 2000:
            continue
        cy, cx = ndimage.center_of_mass(mask)
        col, row = int((sl[1].start + cx) / (w / cols)), int((sl[0].start + cy) / (h / rows))
        # Keep the faint antialiasing around the sprite, not just the thresholded core.
        grown = ndimage.binary_dilation(mask, iterations=3)
        y0, x0 = max(0, sl[0].start - 3), max(0, sl[1].start - 3)
        crop = a[y0:sl[0].stop + 3, x0:sl[1].stop + 3].copy()
        pad = np.zeros(crop.shape[:2], bool)
        pad[sl[0].start - y0:sl[0].start - y0 + grown.shape[0], sl[1].start - x0:sl[1].start - x0 + grown.shape[1]] = grown[:pad.shape[0] - (sl[0].start - y0), :pad.shape[1] - (sl[1].start - x0)]
        crop[~pad] = 0
        if (row, col) not in out or mask.sum() > out[(row, col)][1]:
            out[(row, col)] = (crop, mask.sum())
    return {k: v[0] for k, v in out.items()}


def pack(kind, src, dst):
    cols, rows, cw, ch, feet, head = LAYOUTS[kind]
    a = np.array(Image.open(src).convert("RGBA"))
    a[a[..., 3] < 10] = 0
    sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    ppu = ch * .9 / 2.05 if kind == "enemies" else None
    for (row, col), crop in sorted(frames(a, cols, rows).items()):
        alpha = crop[..., 3] > 40
        ys, xs = np.nonzero(alpha)
        top, bottom = ys.min(), ys.max()
        # A frame taller than the cell is shrunk rather than cropped.
        room = ch - feet - 3
        if bottom - top > room:
            k = room / (bottom - top)
            crop = np.array(Image.fromarray(crop).resize((round(crop.shape[1] * k), round(crop.shape[0] * k)), Image.LANCZOS))
            print(f"  frame row {row} col {col} scaled by {k:.3f}")
            alpha = crop[..., 3] > 40
            ys, xs = np.nonzero(alpha)
            top, bottom = ys.min(), ys.max()
        legs = np.nonzero(alpha[bottom - max(8, int((bottom - top) * .3)):bottom + 1])[1]
        hx = (legs.min() + legs.max()) / 2
        ox = int(round(col * cw + head * cw - hx))
        oy = int(round(row * ch + ch - feet - 1 - bottom))
        # The enemy sheets crop the left 6.5% of each cell (cropLeft): a wide stance moves right to stay visible.
        margin = int(cw * .07) if kind == "enemies" else 0
        if ox + xs.min() < col * cw + margin:
            print(f"  frame row {row} col {col} moved right by {col * cw + margin - ox - xs.min()} px")
            ox = col * cw + margin - xs.min()
        x0, y0 = max(ox, col * cw), max(oy, row * ch)
        x1, y1 = min(ox + crop.shape[1], (col + 1) * cw), min(oy + crop.shape[0], (row + 1) * ch)
        if (x0, y0, x1, y1) != (ox, oy, ox + crop.shape[1], oy + crop.shape[0]):
            print(f"  warning: frame row {row} col {col} clipped by {ox - x0, oy - y0, ox + crop.shape[1] - x1, oy + crop.shape[0] - y1}")
        region = sheet[y0:y1, x0:x1]
        part = crop[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
        region[part[..., 3] > 0] = part[part[..., 3] > 0]
        # Barrel tip: the outermost opaque pixel on the facing side, in the shooting frame.
        cell = sheet[row * ch:(row + 1) * ch, col * cw:(col + 1) * cw, 3] > 128
        ys, xs = np.nonzero(cell)
        side = xs.max() if kind == "enemies" else xs.min()
        tip_y = ys[xs == side].mean()
        px, py = head * cw, ch - feet
        unit = ppu if ppu else ch / 6.6
        print(f"  row {row} col {col}: tip ({(side - px) / unit:.2f}, {(py - tip_y) / unit:.2f}) top {(py - ys.min()) / unit:.2f}"
              f" feet x {(xs[ys > ys.max() - 6].min() - px) / unit:.2f}..{(xs[ys > ys.max() - 6].max() - px) / unit:.2f}")
    Image.fromarray(sheet).save(dst, optimize=True)
    print(dst, sheet.shape[1], "x", sheet.shape[0])


if __name__ == "__main__":
    pack(sys.argv[1], sys.argv[2], sys.argv[3])
