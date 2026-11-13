# Draws the golden woven border used on the trophy room side panels.
# Usage: python make_panel_border.py <output.png>   (requires Pillow)

import math
import sys

from PIL import Image, ImageDraw

# Final image size (2x the 540x900 panel so it stays sharp), and supersampling factor for smooth edges.
WIDTH, HEIGHT = 1080, 1800
SS = 3

INSET = 44            # distance of the plait's centre line from the image edge
CORNER_RADIUS = 80    # rounding of the plait at the corners
AMPLITUDE = 13        # how far each strand swings from the centre line
WAVELENGTH = 84       # approximate length of one full twist
STRAND = 13           # strand thickness
OUTLINE = 4           # dark outline around each strand

DARK = (46, 31, 10, 255)
GOLD_BASE = (168, 116, 34, 255)
GOLD_MID = (214, 165, 72, 255)
GOLD_LIGHT = (246, 223, 150, 255)
CRIMSON = (110, 20, 22, 255)
CRIMSON_LIGHT = (196, 70, 60, 255)


def s(v):
    return v * SS


def sw(v):
    """Supersampled line width (Pillow needs whole numbers)."""
    return max(1, int(round(v * SS)))


def rounded_rect_path(inset, radius, step=1.0):
    """Points (x, y, nx, ny, distance) along a rounded rectangle, clockwise, with outward normals."""
    left, top = inset, inset
    right, bottom = WIDTH - inset, HEIGHT - inset
    pts = []
    dist = 0.0

    def add_line(x0, y0, x1, y1, nx, ny):
        nonlocal dist
        length = math.hypot(x1 - x0, y1 - y0)
        n = max(1, int(length / step))
        for i in range(n):
            t = i / n
            pts.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, nx, ny, dist + length * t))
        dist += length

    def add_arc(cx, cy, a0, a1):
        nonlocal dist
        length = abs(a1 - a0) * radius
        n = max(1, int(length / step))
        for i in range(n):
            a = a0 + (a1 - a0) * i / n
            pts.append((cx + math.cos(a) * radius, cy + math.sin(a) * radius, math.cos(a), math.sin(a), dist + length * i / n))
        dist += length

    add_line(left + radius, top, right - radius, top, 0, -1)
    add_arc(right - radius, top + radius, -math.pi / 2, 0)
    add_line(right, top + radius, right, bottom - radius, 1, 0)
    add_arc(right - radius, bottom - radius, 0, math.pi / 2)
    add_line(right - radius, bottom, left + radius, bottom, 0, 1)
    add_arc(left + radius, bottom - radius, math.pi / 2, math.pi)
    add_line(left, bottom - radius, left, top + radius, -1, 0)
    add_arc(left + radius, top + radius, math.pi, 1.5 * math.pi)
    return pts, dist


def draw_tube(draw, points, width):
    """Draws a shaded golden tube along the given points by stamping discs (outline, base, mid tone, highlight)."""
    for diameter, colour in ((width + OUTLINE * 2, DARK), (width, GOLD_BASE), (width * 0.6, GOLD_MID), (width * 0.22, GOLD_LIGHT)):
        r = diameter * SS / 2
        for x, y in points:
            cx, cy = s(x), s(y)
            draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=colour)


def draw_plait(image):
    """Two strands twisting around the frame path, woven over and under at each crossing."""
    path, perimeter = rounded_rect_path(INSET, CORNER_RADIUS)
    twists = max(4, round(perimeter / WAVELENGTH))
    wavelength = perimeter / twists

    strands = []
    for sign in (1, -1):
        pts = []
        for x, y, nx, ny, d in path:
            offset = sign * AMPLITUDE * math.sin(2 * math.pi * d / wavelength)
            pts.append((x + nx * offset, y + ny * offset))
        pts.append(pts[0])
        strands.append(pts)

    layer_a = Image.new("RGBA", image.size, (0, 0, 0, 0))
    layer_b = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw_tube(ImageDraw.Draw(layer_a), strands[0], STRAND)
    draw_tube(ImageDraw.Draw(layer_b), strands[1], STRAND)

    # Crossings sit every half wavelength; alternate which strand passes over.
    mask = Image.new("L", image.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    radius = s(wavelength * 0.22)
    lookup = {int(round(p[4])): p for p in path}
    for k in range(twists * 2):
        if k % 2 == 1:
            d = int(round(k * wavelength / 2)) % int(perimeter)
            point = lookup.get(d) or min(path, key=lambda p: abs(p[4] - d))
            cx, cy = s(point[0]), s(point[1])
            mask_draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=255)

    a_over = Image.alpha_composite(layer_b, layer_a)
    b_over = Image.alpha_composite(layer_a, layer_b)
    woven = Image.composite(b_over, a_over, mask)
    image.alpha_composite(woven)


def draw_rule(image, inset, radius):
    """Thin golden line following the frame, used either side of the plait."""
    path, _ = rounded_rect_path(inset, radius)
    pts = [(p[0], p[1]) for p in path]
    pts.append(pts[0])
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    scaled = [(s(x), s(y)) for x, y in pts]
    draw.line(scaled, fill=DARK, width=sw(6), joint="curve")
    draw.line(scaled, fill=GOLD_MID, width=sw(3), joint="curve")
    image.alpha_composite(layer)


def draw_medallion(image, cx, cy, radius):
    """Round golden boss with a crimson gem, placed over the plait."""
    draw = ImageDraw.Draw(image)

    def circle(r, fill):
        draw.ellipse((s(cx - r), s(cy - r), s(cx + r), s(cy + r)), fill=fill)

    circle(radius + OUTLINE, DARK)
    circle(radius, GOLD_BASE)
    circle(radius * 0.86, GOLD_MID)
    circle(radius * 0.70, GOLD_BASE)
    circle(radius * 0.62, DARK)
    circle(radius * 0.55, CRIMSON)
    gem = radius * 0.22
    draw.ellipse((s(cx - radius * 0.32), s(cy - radius * 0.32), s(cx - radius * 0.32 + gem), s(cy - radius * 0.32 + gem)), fill=CRIMSON_LIGHT)
    # Small gold studs around the ring.
    for i in range(8):
        a = i * math.pi / 4
        sx, sy = cx + math.cos(a) * radius * 0.78, cy + math.sin(a) * radius * 0.78
        r = radius * 0.08
        draw.ellipse((s(sx - r - 1), s(sy - r - 1), s(sx + r + 1), s(sy + r + 1)), fill=DARK)
        draw.ellipse((s(sx - r), s(sy - r), s(sx + r), s(sy + r)), fill=GOLD_LIGHT)


def main():
    output = sys.argv[1] if len(sys.argv) > 1 else "panel_border.png"
    image = Image.new("RGBA", (s(WIDTH), s(HEIGHT)), (0, 0, 0, 0))

    draw_rule(image, INSET - AMPLITUDE - STRAND - 10, CORNER_RADIUS + AMPLITUDE + STRAND + 10)
    draw_rule(image, INSET + AMPLITUDE + STRAND + 10, max(10, CORNER_RADIUS - AMPLITUDE - STRAND - 10))
    draw_plait(image)

    # Corner medallions sit on the rounded corners; smaller ones mark the middle of each edge.
    corner = INSET + CORNER_RADIUS * (1 - math.cos(math.pi / 4))
    for cx, cy in ((corner, corner), (WIDTH - corner, corner), (corner, HEIGHT - corner), (WIDTH - corner, HEIGHT - corner)):
        draw_medallion(image, cx, cy, 34)
    for cx, cy in ((WIDTH / 2, INSET), (WIDTH / 2, HEIGHT - INSET), (INSET, HEIGHT / 2), (WIDTH - INSET, HEIGHT / 2)):
        draw_medallion(image, cx, cy, 24)

    image = image.resize((WIDTH, HEIGHT), Image.LANCZOS)
    image.save(output)


if __name__ == "__main__":
    main()
