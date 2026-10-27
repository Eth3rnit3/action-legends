#!/usr/bin/env python3
"""Repack a generated sprite sheet so every frame shares the game's pivot.

Each frame is cut from its grid cell (largest connected sprite), cleaned of faint alpha, then
placed in a new grid with its feet on a common baseline and the middle of its legs on the pivot
column: 40% of the cell for enemies, as their "sheet" settings in Resources/Content expect (pivot x .4,
y .045, cropLeft .065), the centre for the mecha. Prints the barrel tips to write as "muzzle" anchors.

usage: pack_sheet.py enemies IN.png OUT.png   (4 x 3 grid -> 4 x 3 cells of 420 x 362)
       pack_sheet.py mecha IN.png OUT.png     (3 x 2 grid -> 3 x 2 cells of 560 x 512)
       pack_sheet.py hero SPEC.json [PREVIEW.png]
The hero mode assembles several generated sheets of one hero into one sheet (see hero() and
tools/art/heroes/*.json): each source is scaled so that its reference frame stands as tall as the
hero should in the game, and the "muzzle" and "hand" anchors are measured on the packed cells.
Requires Pillow, NumPy and SciPy."""
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

LAYOUTS = {
    # cols, rows, cell w, cell h, feet margin (px), pivot column (fraction of cell width)
    "enemies": (4, 3, 420, 362, 16.29, .40),
    "mecha": (3, 2, 560, 512, 12.8, .50),
}


def pieces(a, cols, rows, smallest=150):
    """Every sprite of a grid with its detached bits (a knife, a headband tail): all the pieces whose
    centre falls in a cell, kept together. Returns {(row, col): crop}."""
    h, w = a.shape[:2]
    lab, _ = ndimage.label(a[..., 3] > 40)
    cells = {}
    for i, sl in enumerate(ndimage.find_objects(lab)):
        mask = lab[sl] == i + 1
        if mask.sum() < smallest:
            continue
        cy, cx = ndimage.center_of_mass(mask)
        key = int((sl[0].start + cy) / (h / rows)), int((sl[1].start + cx) / (w / cols))
        cells.setdefault(key, []).append((i + 1, sl, mask.sum()))
    out = {}
    for key, parts in cells.items():
        # Ignore crumbs far smaller than the sprite itself.
        big = max(p[2] for p in parts)
        parts = [p for p in parts if p[2] >= big * .002]
        keep = np.isin(lab, [p[0] for p in parts])
        keep = ndimage.binary_dilation(keep, iterations=3) & (a[..., 3] > 0)
        ys, xs = np.nonzero(keep)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        crop = a[y0:y1, x0:x1].copy()
        crop[~keep[y0:y1, x0:x1]] = 0
        out[key] = crop
    return out


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


def tip(alpha, direction):
    """Barrel tip (or hand) of a packed cell, in cell pixels: the extreme opaque pixel in a direction."""
    ys, xs = np.nonzero(alpha)
    top, bottom, left, right = ys.min(), ys.max(), xs.min(), xs.max()
    if direction == "right":
        # The gun is the forward-most thing above the hips; a stride's front foot is lower.
        keep = ys <= top + (bottom - top) * .62
        x = xs[keep].max()
        return x, ys[keep][xs[keep] >= x - 2].mean()
    if direction == "up":
        y = top
        return xs[ys <= y + 2].mean(), y
    if direction == "down":
        # Pointed down between the legs: the lowest pixel near the middle of the sprite.
        mid = (left + right) / 2
        keep = np.abs(xs - mid) <= (right - left) * .14
        y = ys[keep].max()
        return xs[keep][ys[keep] >= y - 2].mean(), y
    if direction in ("diagonal", "hand"):
        score = xs - ys
        best = score.max()
        sel = score >= best - 3
        return xs[sel].mean(), ys[sel].mean()
    raise ValueError(direction)


def hero(spec_path, preview=None):
    """SPEC: {"output", "columns", "rows", "cell": [w, h], "feet", "pivotX", "ppu", "height",
    "sources": {"A": {"image", "grid": [cols, rows], "reference": frame}}, "frames": [["A", frame, dx?], ...],
    "anchors": {"muzzle": {"cell": direction}, "hand": {"cell": "hand"}}}. Paths are relative to SPEC."""
    spec_path = Path(spec_path)
    spec = json.loads(spec_path.read_text())
    base = spec_path.parent
    cols, rows = spec["columns"], spec["rows"]
    cw, ch = spec["cell"]
    feet, ppu = spec["feet"], spec["ppu"]
    px, py = spec["pivotX"] * cw, ch - feet
    sources = {}
    for key, source in spec["sources"].items():
        a = np.array(Image.open(base / source["image"]).convert("RGBA"))
        a[a[..., 3] < 10] = 0
        found = pieces(a, *source["grid"])
        gc = source["grid"][0]
        ref = found[divmod(source["reference"], gc)]
        ys = np.nonzero(ref[..., 3] > 40)[0]
        k = spec["height"] * ppu / (ys.max() - ys.min())
        print(f"source {key}: {len(found)} frames, scale {k:.3f}")
        sources[key] = ({r * gc + c: f for (r, c), f in found.items()}, k)
    sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for index, entry in enumerate(spec["frames"]):
        key, frame = entry[0], entry[1]
        found, k = sources[key]
        if frame not in found:
            print(f"  warning: frame {key}{frame} not found")
            continue
        crop = found[frame]
        crop = np.array(Image.fromarray(crop).resize((max(1, round(crop.shape[1] * k)), max(1, round(crop.shape[0] * k))), Image.LANCZOS))
        alpha = crop[..., 3] > 40
        ys, xs = np.nonzero(alpha)
        top, bottom = ys.min(), ys.max()
        legs = np.nonzero(alpha[bottom - max(8, int((bottom - top) * .3)):bottom + 1])[1]
        row, col = divmod(index, cols)
        ox = int(round(col * cw + px - (legs.min() + legs.max()) / 2 + (entry[2] if len(entry) > 2 else 0)))
        oy = int(round(row * ch + py - 1 - bottom))
        ox = max(ox, col * cw + 2 - xs.min())
        ox = min(ox, (col + 1) * cw - 3 - xs.max())
        if oy + top < row * ch:
            print(f"  warning: cell {index} ({key}{frame}) clipped at the top by {row * ch - oy - top} px")
        x0, y0 = max(ox, col * cw), max(oy, row * ch)
        x1, y1 = min(ox + crop.shape[1], (col + 1) * cw), min(oy + crop.shape[0], (row + 1) * ch)
        region = sheet[y0:y1, x0:x1]
        part = crop[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
        region[part[..., 3] > 0] = part[part[..., 3] > 0]
    Image.fromarray(sheet).save(base / spec["output"], optimize=True)
    print(base / spec["output"], sheet.shape[1], "x", sheet.shape[0], f"cellHeight {ch / ppu:.7f} pivot ({spec['pivotX']}, {feet / ch:.7f})")
    shown = Image.fromarray(sheet.copy()) if preview else None
    if shown:
        from PIL import ImageDraw
        draw = ImageDraw.Draw(shown)
    anchors = []
    for name, cells in spec["anchors"].items():
        for cell, direction in sorted(cells.items(), key=lambda c: int(c[0])):
            row, col = divmod(int(cell), cols)
            alpha = sheet[row * ch:(row + 1) * ch, col * cw:(col + 1) * cw, 3] > 128
            x, y = tip(alpha, direction)
            anchors.append({"name": name, "cell": int(cell), "x": round((x - px) / ppu, 2), "y": round((py - y) / ppu, 2)})
            if shown:
                cx, cy = col * cw + x, row * ch + y
                draw.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), outline=(255, 0, 255, 255), width=2)
    if shown:
        for c in range(cols + 1):
            draw.line((c * cw, 0, c * cw, rows * ch), fill=(0, 255, 255, 120))
        for r in range(rows):
            draw.line((0, r * ch + py, cols * cw, r * ch + py), fill=(255, 255, 0, 160))
        shown.save(preview)
    print(json.dumps(anchors))


if __name__ == "__main__":
    if sys.argv[1] == "hero":
        hero(sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
    else:
        pack(sys.argv[1], sys.argv[2], sys.argv[3])
