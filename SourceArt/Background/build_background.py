"""Batch 4b: background tile (seamless, opaque, very subtle paint-factory motifs). 256 px = 1 unit."""
import sys, os, asyncio
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_meta import meta_png, guid_for
from playwright.async_api import async_playwright

OUT = sys.argv[1]
SRC = os.path.join(OUT, "SourceArt", "Background")
PNG = os.path.join(OUT, "Assets", "_Project", "Sprites", "Background")
os.makedirs(SRC, exist_ok=True); os.makedirs(PNG, exist_ok=True)

BG = "#E3EEF6"; MOTIF = "#D3E2EE"
T = 512  # tile size in px (2 units), drawn so it repeats seamlessly

def drop(x, y, s, rot):
    # paint drop, same family as the items
    return (f'<path transform="translate({x},{y}) rotate({rot}) scale({s})" '
            f'd="M0,-14 C6,-6 10,0 10,5 A10,10 0 0 1 -10,5 C-10,0 -6,-6 0,-14Z"/>')

def dot(x, y, r): return f'<circle cx="{x}" cy="{y}" r="{r}"/>'

def svg_tile():
    shapes = []
    # motif positions inside the tile; anything crossing an edge is repeated on the opposite side
    motifs = [("d", 70, 90, 1.6, -15), ("d", 330, 60, 1.2, 20), ("d", 200, 260, 1.9, 8), ("d", 440, 300, 1.4, -25),
              ("d", 110, 420, 1.3, 30), ("d", 380, 470, 1.7, -5),
              ("o", 250, 120, 7), ("o", 470, 170, 5), ("o", 40, 250, 6), ("o", 300, 390, 8), ("o", 190, 480, 5), ("o", 500, 420, 4)]
    for m in motifs:
        for dx in (-T, 0, T):
            for dy in (-T, 0, T):
                if m[0] == "d": shapes.append(drop(m[1] + dx, m[2] + dy, m[3], m[4]))
                else: shapes.append(dot(m[1] + dx, m[2] + dy, m[3]))
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{T}" height="{T}" viewBox="0 0 {T} {T}">
  <rect width="{T}" height="{T}" fill="{BG}"/>
  <g fill="{MOTIF}">{"".join(shapes)}</g>
</svg>'''

async def render():
    async with async_playwright() as p:
        b = await p.chromium.launch()
        page = await b.new_page(viewport={"width": T, "height": T})
        name = "Background_Tile"; svg = svg_tile()
        open(os.path.join(SRC, name + ".svg"), "w").write(svg)
        await page.set_content(f'<html><body style="margin:0">{svg}</body></html>')
        await page.locator("svg").screenshot(path=os.path.join(PNG, name + ".png"))
        meta = meta_png(guid_for(name)).replace("    wrapU: 1\n    wrapV: 1\n    wrapW: 1", "    wrapU: 0\n    wrapV: 0\n    wrapW: 0")
        open(os.path.join(PNG, name + ".png.meta"), "w").write(meta)
        print(name, guid_for(name))
        await b.close()

asyncio.run(render())
