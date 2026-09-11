"""Independent Python check for shared sketch-edge fixtures (not a COM executor)."""
import math


def measure_rectangle(edges):
    if len(edges) != 4:
        return None
    points = [(e[f'x{i}'], e[f'y{i}'], e[f'z{i}']) for e in edges for i in (1, 2)]
    if not all(math.isfinite(value) for point in points for value in point):
        return None
    left, right = min(p[0] for p in points), max(p[0] for p in points)
    bottom, top = min(p[1] for p in points), max(p[1] for p in points)
    width, height = right - left, top - bottom
    if not (0 < width <= 10000 and 0 < height <= 10000):
        return None
    tolerance = min(1e-6, min(width, height) * 1e-6)
    corners = [(left, bottom), (right, bottom), (right, top), (left, top)]
    boundary = {frozenset((i, (i + 1) % 4)) for i in range(4)}
    seen = set()
    for index in range(0, len(points), 2):
        ids = []
        for x, y, z in points[index:index+2]:
            if abs(z) > tolerance:
                return None
            matches = [i for i, (cx, cy) in enumerate(corners)
                       if abs(x-cx) <= tolerance and abs(y-cy) <= tolerance]
            if len(matches) != 1:
                return None
            ids.append(matches[0])
        edge = frozenset(ids)
        if edge not in boundary or edge in seen:
            return None
        seen.add(edge)
    return (width, height) if seen == boundary else None
