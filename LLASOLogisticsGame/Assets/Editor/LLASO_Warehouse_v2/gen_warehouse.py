import math, collections, numpy as np

# ------------------------------------------------------------------ core
class Model:
    def __init__(s):
        s.objs = collections.OrderedDict()   # name -> list of (mat, [tri verts], normal)
        s.boxes = []
    def tri(s, obj, mat, a, b, c, n):
        s.objs.setdefault(obj, []).append((mat, (a, b, c), n))
    def poly(s, obj, mat, pts, n):
        """pts: planar convex polygon; winding auto-corrected so geometric normal ~ n"""
        pts = [tuple(map(float, p)) for p in pts]
        p0, p1, p2 = map(np.array, pts[:3])
        if np.dot(np.cross(p1 - p0, p2 - p0), n) < 0:
            pts = pts[::-1]
        for i in range(1, len(pts) - 1):
            s.tri(obj, mat, pts[0], pts[i], pts[i + 1], n)

    def box(s, obj, mat, x0, x1, y0, y1, z0, z1):
        if x1 - x0 < 1e-6 or y1 - y0 < 1e-6 or z1 - z0 < 1e-6:
            return
        s.boxes.append((obj, mat, x0, x1, y0, y1, z0, z1))
        P = lambda x, y, z: (x, y, z)
        s.poly(obj, mat, [P(x1,y0,z0),P(x1,y1,z0),P(x1,y1,z1),P(x1,y0,z1)], (1,0,0))
        s.poly(obj, mat, [P(x0,y0,z0),P(x0,y1,z0),P(x0,y1,z1),P(x0,y0,z1)], (-1,0,0))
        s.poly(obj, mat, [P(x0,y1,z0),P(x1,y1,z0),P(x1,y1,z1),P(x0,y1,z1)], (0,1,0))
        s.poly(obj, mat, [P(x0,y0,z0),P(x1,y0,z0),P(x1,y0,z1),P(x0,y0,z1)], (0,-1,0))
        s.poly(obj, mat, [P(x0,y0,z1),P(x1,y0,z1),P(x1,y1,z1),P(x0,y1,z1)], (0,0,1))
        s.poly(obj, mat, [P(x0,y0,z0),P(x1,y0,z0),P(x1,y1,z0),P(x0,y1,z0)], (0,0,-1))

    def prism_z(s, obj, mat, poly_xy, z0, z1):
        """convex XY polygon extruded along Z"""
        cx = sum(p[0] for p in poly_xy) / len(poly_xy)
        cy = sum(p[1] for p in poly_xy) / len(poly_xy)
        n = len(poly_xy)
        for i in range(n):
            a, b = poly_xy[i], poly_xy[(i + 1) % n]
            ex, ey = b[0] - a[0], b[1] - a[1]
            nx, ny = ey, -ex
            mx, my = (a[0] + b[0]) / 2 - cx, (a[1] + b[1]) / 2 - cy
            if nx * mx + ny * my < 0: nx, ny = -nx, -ny
            l = math.hypot(nx, ny) or 1
            s.poly(obj, mat, [(a[0],a[1],z0),(b[0],b[1],z0),(b[0],b[1],z1),(a[0],a[1],z1)], (nx/l, ny/l, 0))
        s.poly(obj, mat, [(p[0],p[1],z1) for p in poly_xy], (0,0,1))
        s.poly(obj, mat, [(p[0],p[1],z0) for p in poly_xy], (0,0,-1))

    def wall(s, obj, mat, axis, d0, d1, u0, u1, y0, y1, openings=()):
        """axis 'z': wall plane normal=z, runs along x (u=x), depth z in [d0,d1].
           axis 'x': wall plane normal=x, runs along z (u=z), depth x in [d0,d1].
           openings: (ua, ub, ya, yb) rectangles carved out."""
        cuts = sorted({u0, u1, *[o[0] for o in openings if u0 < o[0] < u1], *[o[1] for o in openings if u0 < o[1] < u1]})
        cols = []
        for a, b in zip(cuts, cuts[1:]):
            m = (a + b) / 2
            free = [(y0, y1)]
            for (ua, ub, ya, yb) in openings:
                if ua < m < ub:
                    nf = []
                    for (f0, f1) in free:
                        if yb <= f0 or ya >= f1: nf.append((f0, f1)); continue
                        if ya > f0: nf.append((f0, ya))
                        if yb < f1: nf.append((yb, f1))
                    free = nf
            cols.append((a, b, free))
        merged = []
        for a, b, free in cols:
            for fy in free:
                for m in merged:
                    if m[3] == fy and abs(m[1] - a) < 1e-9:
                        m[1] = b; break
                else:
                    merged.append([a, b, None, fy])
        for a, b, _, (f0, f1) in merged:
            if axis == 'z': s.box(obj, mat, a, b, f0, f1, d0, d1)
            else:           s.box(obj, mat, d0, d1, f0, f1, a, b)

    # ------------------------------------------------------------------ output
    def write(s, path_obj, mtl_name, mirrored, header):
        verts, norms = [], []
        vi, ni = {}, {}
        def V(p):
            p = (-p[0] if mirrored else p[0], p[1], p[2])
            k = tuple(round(c, 4) for c in p)
            if k not in vi: vi[k] = len(verts) + 1; verts.append(k)
            return vi[k]
        def N(n):
            n = (-n[0] if mirrored else n[0], n[1], n[2])
            k = tuple(round(c, 4) for c in n)
            if k not in ni: ni[k] = len(norms) + 1; norms.append(k)
            return ni[k]
        body = []
        for name, tris in s.objs.items():
            body.append(f'o {name}')
            cur = None
            for mat, (a, b, c), n in tris:
                if mat != cur: body.append(f'usemtl {mat}'); cur = mat
                ids = [V(a), V(b), V(c)]
                if mirrored: ids = ids[::-1]
                nn = N(n)
                body.append(f'f {ids[0]}//{nn} {ids[1]}//{nn} {ids[2]}//{nn}')
        with open(path_obj, 'w') as f:
            f.write(header.rstrip() + '\n')
            f.write(f'mtllib {mtl_name}\n')
            for v in verts: f.write('v %.4f %.4f %.4f\n' % v)
            for n in norms: f.write('vn %.4f %.4f %.4f\n' % n)
            f.write('\n'.join(body) + '\n')
        return len(verts), sum(len(t) for t in s.objs.values())


# ------------------------------------------------------------------ design
m = Model()
HW   = 13.0          # half-width  (26 m wide)
Z0   = -0.25         # hall end-face (shared with facility)
LEN  = 30.0
Z1   = Z0 - LEN      # dock wall outer face  (-30.25)
T    = 0.4           # wall thickness
H    = 9.6           # eave height (matches facility rooms)
RISE = 2.0           # barrel-roof rise
RW   = HW + 0.3      # roof half-width incl. overhang
def arch(x): return H - 0.1 + RISE * (1 - (x / RW) ** 2)       # roof centre-line; base sits just inside the wall tops
RB = [(-RW + i * (2 * RW) / 16) for i in range(17)]            # roof break points
def under(x):                                                  # roof underside (piece-wise linear, same chain the roof uses)
    for a, b in zip(RB, RB[1:]):
        if a <= x <= b:
            ya, yb = arch(a) - 0.05, arch(b) - 0.05
            return ya + (yb - ya) * (x - a) / (b - a)
    raise ValueError(x)
def chain(x0, x1):
    return [x0] + [b for b in RB if x0 < b < x1] + [x1]

W,G,DB,PU,LB,GR,DS,RF,GL,FL,LT = ('Fac_White','Fac_Grey','Fac_DarkBlue','Fac_Purple','Fac_LightBlue','Fac_Green',
                                   'Fac_DarkSteel','Fac_Roof','Fac_Glass','Fac_Floor','Fac_Light')
CON, YEL = 'Fac_Concrete', 'Fac_Yellow'
ZF, ZC, ZI = 'Zone_Fragile', 'Zone_Cold', 'Zone_Inspection'
E_ = 0.01   # band clearance so trim never shares a plane with the wall behind it

def banded(obj, axis, d0, d1, u0, u1, openings, out=0.05, inn=0.05):
    """base band (dark blue) + stripe (light blue), wrapped around a wall, stopped 1 cm short of every edge/jamb."""
    ops = [(a - E_, b + E_, ya, yb) for (a, b, ya, yb) in openings]
    m.wall(obj, DB, axis, d0 - out, d1 + inn, u0 + E_, u1 - E_, 0.0, 0.9, ops)
    m.wall(obj, LB, axis, d0 - out, d1 + inn, u0 + E_, u1 - E_, 0.9, 1.1, ops)

# ---- floor
m.box('LLASO_WH_Floor', CON, -HW, HW, -0.6, 0.0, Z1, Z0)

# ---- dock wall with 4 truck bays
BAY_W, BAY_H = 3.8, 4.4
bay_x = [-9.75, -3.25, 3.25, 9.75]
bays  = [(cx - BAY_W/2, cx + BAY_W/2, 0.0, BAY_H) for cx in bay_x]
dz0, dz1 = Z1, Z1 + T
m.wall('LLASO_WH_DockWall', W, 'z', dz0, dz1, -HW, HW, 0, H+0.1, bays)
banded('LLASO_WH_DockWall', 'z', dz0, dz1, -HW, HW, bays, out=0.05, inn=0.05)

# ---- hall-side wall (shares the facility end-face) with the 12 m opening
hz0, hz1 = Z0 - T, Z0
hall_open = [(-6.0, 6.0, 0.0, 4.25)]
m.wall('LLASO_WH_HallWall', W, 'z', hz0, hz1, -HW, HW, 0, H+0.1, hall_open)
banded('LLASO_WH_HallWall', 'z', hz0, hz1, -HW, HW, hall_open, out=0.05, inn=-0.02)   # inner side only (outer side is the facility)
# purple frame: pushed 4 cm into the opening so it never shares a plane with the jamb
m.box('LLASO_WH_HallFrame', PU, -6.35, -5.96, 0, 4.6, hz0-0.35, hz1-0.03)
m.box('LLASO_WH_HallFrame', PU,  5.96,  6.35, 0, 4.6, hz0-0.35, hz1-0.03)
m.box('LLASO_WH_HallFrame', PU, -6.35, 6.35, 4.21, 4.6, hz0-0.35, hz1-0.03)

# ---- side walls
LINK_Z, LINK_W, LINK_H, LINK_L = -8.5, 9.6, 4.4, 5.2
ctb = (LINK_Z - LINK_W/2, LINK_Z + LINK_W/2, 0.0, LINK_H)
exit_door = (-27.4, -25.8, 0.0, 2.6)
windows_z = [(-7.0, -2.0), (-14.0, -9.0), (-21.0, -16.0), (-28.0, -23.0)]
WY0, WY1 = 6.0, 8.2
win_open = [(a, b, WY0, WY1) for a, b in windows_z]
sz0, sz1 = Z1 + T, hz0
for side, x0, x1, extra in (('N', HW - T, HW, [ctb]), ('S', -HW, -HW + T, [exit_door])):
    nm = 'LLASO_WH_SideWall_' + ('CTB' if side == 'N' else 'Fragile')
    ops = extra + win_open
    m.wall(nm, W, 'x', x0, x1, sz0, sz1, 0, H+0.1, ops)
    # bands stop 6 cm short of the dock/hall walls so they never overlap those walls' bands at the corners
    banded(nm, 'x', x0, x1, sz0 + 0.06, sz1 - 0.06, extra + win_open, out=0.05, inn=0.05)
    xm = (x0 + x1) / 2
    for a, b in windows_z:
        m.box('LLASO_WH_Windows', GL, xm-0.03, xm+0.03, WY0+0.12, WY1-0.12, a+0.0, b-0.0)
        m.box('LLASO_WH_Windows', DS, x0-0.04, x1+0.04, WY0-0.04, WY0+0.12, a-0.04, b+0.04)
        m.box('LLASO_WH_Windows', DS, x0-0.04, x1+0.04, WY1-0.12, WY1+0.04, a-0.04, b+0.04)
        for zz in (a-0.04, b-0.04+0.04-0.0):
            pass
        m.box('LLASO_WH_Windows', DS, x0-0.04, x1+0.04, WY0+0.12, WY1-0.12, a-0.04, a+0.1)
        m.box('LLASO_WH_Windows', DS, x0-0.04, x1+0.04, WY0+0.12, WY1-0.12, b-0.1, b+0.04)
        mid = (a + b) / 2
        m.box('LLASO_WH_Windows', DS, x0-0.04, x1+0.04, WY0+0.12, WY1-0.12, mid-0.05, mid+0.05)
# green emergency exit frame (4 cm into the opening, header lowered)
ed0, ed1 = exit_door[0], exit_door[1]
m.box('LLASO_WH_ExitDoor', GR, -HW-0.06, -HW+T+0.06, 0, 2.64, ed0-0.12, ed0+0.04)
m.box('LLASO_WH_ExitDoor', GR, -HW-0.06, -HW+T+0.06, 0, 2.64, ed1-0.04, ed1+0.12)
m.box('LLASO_WH_ExitDoor', GR, -HW-0.06, -HW+T+0.06, 2.56, 2.76, ed0-0.12, ed1+0.12)

# ---- gable / barrel-vault fill above the end walls (follows the roof chain, 5 cm embedded into the roof)
px = chain(-HW, HW)
ped = [(-HW, H - 0.05), (HW, H - 0.05)] + [(x, under(x) + 0.05) for x in reversed(px)]
m.prism_z('LLASO_WH_Pediment', W, ped, dz0, dz1)
m.prism_z('LLASO_WH_Pediment', W, ped, hz0, hz1)

# ---- roof (barrel vault shell). Dock-side fascia is adjacent to - not overlapping - the roof
for a, b in zip(RB, RB[1:]):
    seg = [(a, arch(a)-0.05), (b, arch(b)-0.05), (b, arch(b)+0.30), (a, arch(a)+0.30)]
    m.prism_z('LLASO_WH_Roof', RF, seg, Z1-0.2, Z0)
    m.prism_z('LLASO_WH_Roof', DB, seg, Z1-0.4, Z1-0.2)

# ---- steel ribs, spaced to miss every window, door and zone wall; pilaster only where the wall is solid
RIBS = (-8.0, -15.0, -22.6, -29.0)
rc = chain(-12.9, 12.9)
for z in RIBS:
    for a, b in zip(rc, rc[1:]):
        m.prism_z('LLASO_WH_Ribs', DS, [(a, under(a)-0.4), (b, under(b)-0.4), (b, under(b)), (a, under(a))], z-0.15, z+0.15)
    for x0, x1, side in ((-HW+T, -HW+T+0.3, 'S'), (HW-T-0.3, HW-T, 'N')):
        ybot = 0.0
        if side == 'N' and ctb[0] < z < ctb[1]: ybot = LINK_H + 0.3      # corbel over the CTB doorway (sits on its frame)
        m.box('LLASO_WH_Ribs', DS, x0, x1, ybot, H, z-0.15, z+0.15)

# ---- light bars (emissive) + hangers
light_pos = []
for x in (-7.0, 0.0, 7.0):
    for zc in (-4.5, -10.0, -15.5, -21.0, -26.5):
        y = arch(x) - 1.9
        m.box('LLASO_WH_LightBars', LT, x-0.15, x+0.15, y, y+0.12, zc-2.0, zc+2.0)
        m.box('LLASO_WH_LightBars', DS, x-0.02, x+0.02, y+0.12, under(x)-0.0, zc-0.02, zc+0.02)
        if (x != 0.0 and zc in (-4.5, -15.5, -26.5)) or (x == 0.0 and zc in (-10.0, -21.0)):
            light_pos.append((x, y - 0.3, zc))

# ---- dock hardware per bay
for cx in bay_x:
    xa, xb = cx - BAY_W/2, cx + BAY_W/2
    # steel dock frame, 4 cm into the opening on every side
    m.box('LLASO_WH_DockFrames', DS, xa-0.28, xa+0.04, 0, BAY_H+0.3, Z1-0.25, Z1+T+0.12)
    m.box('LLASO_WH_DockFrames', DS, xb-0.04, xb+0.28, 0, BAY_H+0.3, Z1-0.25, Z1+T+0.12)
    m.box('LLASO_WH_DockFrames', DS, xa+0.04, xb-0.04, BAY_H-0.04, BAY_H+0.3, Z1-0.25, Z1+T+0.12)
    # rolled-up door housing (inside)
    m.box('LLASO_WH_DockDoors', G, xa-0.1, xb+0.1, BAY_H+0.3, BAY_H+0.95, Z1+T+0.12, Z1+T+0.6)
    # bumpers: bolted to the wall on the outside of each frame (previously floated in front of the opening)
    for bx0, bx1 in ((xa-0.68, xa-0.28), (xb+0.28, xb+0.68)):
        m.box('LLASO_WH_Bumpers', DS, bx0, bx1, 1.0, 1.6, Z1-0.3, Z1)
    # leveler bridges the doorway; hazard lip on the inside edge
    m.box('LLASO_WH_Levelers', DS, xa+0.15, xb-0.15, 0.0, 0.07, Z1, Z1+T+2.2)
    m.box('LLASO_WH_Levelers', YEL, xa+0.15, xb-0.15, 0.0, 0.07, Z1+T+2.2, Z1+T+2.5)
    # exterior bay plate, status light, dock lamp
    m.box('LLASO_WH_BayPlates', PU, cx-0.7, cx+0.7, 5.5, 6.1, Z1-0.07, Z1)
    m.box('LLASO_WH_BayPlates', GR, xb+0.35, xb+0.6, 2.6, 2.85, Z1-0.12, Z1)
    m.box('LLASO_WH_DockLights', DS, cx-0.05, cx+0.05, 5.05, 5.12, Z1-0.9, Z1)
    m.box('LLASO_WH_DockLights', LT, cx-0.3, cx+0.3, 4.98, 5.05, Z1-1.0, Z1-0.7)

# ---- exterior: canopy (fascia adjacent to slab), apron, paint, accents
CAN_D = 5.0
m.box('LLASO_WH_Canopy', RF, -HW-0.3, HW+0.3, 6.35, 6.65, Z1-CAN_D+0.2, Z1)
m.box('LLASO_WH_Canopy', DB, -HW-0.3, HW+0.3, 6.35, 6.65, Z1-CAN_D, Z1-CAN_D+0.2)
for x in (-HW, -6.5, 0.0, 6.5, HW):
    m.box('LLASO_WH_Canopy', DS, x-0.15, x+0.15, 0, 6.35, Z1-CAN_D+0.3, Z1-CAN_D+0.6)
APR = 24.0
m.box('LLASO_WH_Apron', CON, -HW-2, HW+2, -0.6, 0.02, Z1-APR, Z1)
for x in (-HW, -6.5, 0.0, 6.5, HW):
    m.box('LLASO_WH_ApronPaint', W, x-0.08, x+0.08, 0.02, 0.04, Z1-APR+1, Z1-0.3)
for cx in bay_x:
    m.box('LLASO_WH_ApronPaint', YEL, cx-2.6, cx+2.6, 0.02, 0.04, Z1-APR+0.8, Z1-APR+1.1)
    m.box('LLASO_WH_ApronPaint', GR, cx-0.1, cx+0.1, 0.02, 0.04, Z1-APR+1.3, Z1-7.0)
# accents: purple blade rises from above the canopy; green corner bars start under the canopy line and stop above it
m.box('LLASO_WH_Accents', PU, -0.5, 0.5, 6.7, H, Z1-0.1, Z1)
for x in (-HW, HW-0.3):
    m.box('LLASO_WH_Accents', GR, x, x+0.3, 1.1, 6.35, Z1-0.06, Z1)
    m.box('LLASO_WH_Accents', GR, x, x+0.3, 6.65, H, Z1-0.06, Z1)

# ---- CTB / Rack Packing link corridor (ceiling sits between the walls; end cap is separate)
lx0, lx1 = HW, HW + LINK_L
za, zb = LINK_Z - LINK_W/2, LINK_Z + LINK_W/2
m.box('LLASO_WH_CTBLink', FL, lx0, lx1, -0.6, 0.0, za-0.3, zb+0.3)
for (a, b, d0, d1) in ((za-0.3, za, a if False else za-0.34, za+0.03), (zb, zb+0.3, zb-0.03, zb+0.34)):
    m.box('LLASO_WH_CTBLink', W, lx0, lx1-0.3, 0, LINK_H, a, b)
    m.box('LLASO_WH_CTBLink', DB, lx0, lx1-0.32, 0, 0.9, d0, d1)
m.box('LLASO_WH_CTBLink', RF, lx0, lx1-0.3, LINK_H-0.3, LINK_H, za, zb)
m.box('LLASO_WH_CTBLink', LT, lx0+0.5, lx1-0.8, LINK_H-0.36, LINK_H-0.3, LINK_Z-0.2, LINK_Z+0.2)
m.box('LLASO_WH_CTBLink', PU, lx0, lx1-0.55, 0.0, 0.03, LINK_Z-0.4, LINK_Z+0.4)
m.box('LLASO_WH_CTBLink', W, lx1-0.3, lx1, 0, LINK_H, za-0.3, zb+0.3)                 # end cap
m.box('LLASO_WH_CTBLink', PU, lx1-0.45, lx1-0.3, 0, 3.5, LINK_Z-2.3, LINK_Z+2.3)
m.box('LLASO_WH_CTBLink', LB, lx1-0.55, lx1-0.45, 0, 3.2, LINK_Z-2.0, LINK_Z-0.05)
m.box('LLASO_WH_CTBLink', LB, lx1-0.55, lx1-0.45, 0, 3.2, LINK_Z+0.05, LINK_Z+2.0)
# purple frame on the warehouse side of the doorway (inside the building, flush against the wall)
m.box('LLASO_WH_CTBFrame', PU, HW-T-0.3, HW-T, 0, LINK_H+0.3, za-0.25, za+0.04)
m.box('LLASO_WH_CTBFrame', PU, HW-T-0.3, HW-T, 0, LINK_H+0.3, zb-0.04, zb+0.25)
m.box('LLASO_WH_CTBFrame', PU, HW-T-0.3, HW-T, LINK_H-0.04, LINK_H+0.3, za+0.04, zb-0.04)

# ---- floor paint (each zone uses its own height so crossings never share a plane)
def paint(obj, mat, x0, x1, z0, z1, w=0.2, h=0.03):
    m.box(obj, mat, x0, x1, 0, h, z0, z0+w); m.box(obj, mat, x0, x1, 0, h, z1-w, z1)
    m.box(obj, mat, x0, x0+w, 0, h, z0+w, z1-w); m.box(obj, mat, x1-w, x1, 0, h, z0+w, z1-w)   # side strips between the end strips

FR = (-HW+T, -6.6, -11.6, -1.2)
CO = (-HW+T, -6.6, -22.4, -12.2)
IN = (-HW+T,  HW-T, -29.85, -23.4)
ST = (6.8,  HW-T-0.45, -14.8, -2.2)
paint('LLASO_WH_Zone_Fragile', ZF, *FR)
paint('LLASO_WH_Zone_Cold', ZC, *CO)
paint('LLASO_WH_Zone_Inspection', ZI, *IN)
paint('LLASO_WH_Zone_Staging', PU, *ST, w=0.15)
m.box('LLASO_WH_Aisle', GR, -5.5, -5.3, 0, 0.03, -23.0, -0.9)
m.box('LLASO_WH_Aisle', GR,  5.3,  5.5, 0, 0.03, -23.0, -0.9)
for cx in bay_x:       # yellow bay lanes: start inside the inspection border so they sit on top of it (0.035 > 0.03)
    for lx in (cx-1.9, cx+1.75):
        m.box('LLASO_WH_Aisle', YEL, lx, lx+0.15, 0, 0.035, IN[2]+0.2, IN[3]-0.2)

# ---- cold storage room: walls inset 0.4 m from the painted border so the border stays visible
cw, PAD = 0.2, 0.4
cx0, cx1, cz0, cz1 = CO[0], CO[1]-PAD, CO[2]+PAD, CO[3]-PAD
CZM = (cz0 + cz1) / 2
cold_door = (CZM-1.6, CZM+1.6, 0.0, 3.0)
m.wall('LLASO_WH_ColdStorage', W, 'z', cz1-cw, cz1, cx0, cx1, 0, 4.4)
m.wall('LLASO_WH_ColdStorage', W, 'z', cz0, cz0+cw, cx0, cx1, 0, 4.4)
m.wall('LLASO_WH_ColdStorage', W, 'x', cx1-cw, cx1, cz0+cw, cz1-cw, 0, 4.4, [cold_door])
for (zA, zB) in ((cz1-cw-0.03, cz1+0.03), (cz0-0.03, cz0+cw+0.03)):          # dark base strip + light rim, ends overhang the wall
    m.box('LLASO_WH_ColdStorage', DB, cx0, cx1+0.03, 0, 0.4, zA, zB)
    m.box('LLASO_WH_ColdStorage', LB, cx0, cx1+0.03, 4.4, 4.55, zA+0.0, zB+0.0)
m.box('LLASO_WH_ColdStorage', DB, cx1-cw-0.03, cx1+0.03, 0, 0.4, cz0+cw, cold_door[0]-0.01)
m.box('LLASO_WH_ColdStorage', DB, cx1-cw-0.03, cx1+0.03, 0, 0.4, cold_door[1]+0.01, cz1-cw)
m.box('LLASO_WH_ColdStorage', LB, cx1-cw-0.03, cx1+0.03, 4.4, 4.55, cz0+cw, cz1-cw)
for (a, b) in ((cold_door[0]-0.15, cold_door[0]+0.04), (cold_door[1]-0.04, cold_door[1]+0.15)):
    m.box('LLASO_WH_ColdDoor', DB, cx1-cw-0.05, cx1+0.05, 0.4, 3.15, a, b)
m.box('LLASO_WH_ColdDoor', DB, cx1-cw-0.05, cx1+0.05, 2.96, 3.15, cold_door[0]+0.04, cold_door[1]-0.04)

# ---- fragile storage: waist-high glass partitions, inset like the cold room
fx0, fx1, fz0, fz1 = FR[0], FR[1]-PAD, FR[2]+PAD, FR[3]-PAD
FZM = (fz0 + fz1) / 2
GAP = 1.75
def glass_run(axis, d0, d1, u0, u1, gaps=()):
    # glass 1.2 m, steel top rail and foot, all sharing the same footprint but at different heights (no shared planes)
    m.wall('LLASO_WH_FragileStorage', GL, axis, d0, d1, u0, u1, 0.15, 1.2, gaps)
    m.wall('LLASO_WH_FragileStorage', DS, axis, d0-0.03, d1+0.03, u0, u1, 1.2, 1.3, gaps)
    m.wall('LLASO_WH_FragileStorage', DS, axis, d0-0.03, d1+0.03, u0, u1, 0.0, 0.15, gaps)
glass_run('z', fz1-0.06, fz1, fx0, fx1)
glass_run('z', fz0, fz0+0.06, fx0, fx1)
glass_run('x', fx1-0.06, fx1, fz0, fz1, [(FZM-GAP, FZM+GAP, 0, 1.3)])
for pz in (fz0+0.03, fz1-0.03, FZM-GAP, FZM+GAP):
    m.box('LLASO_WH_FragileStorage', DS, fx1-0.1, fx1+0.04, 0, 1.4, pz-0.06, pz+0.06)
for pz in (fz0+0.03, fz1-0.03):
    m.box('LLASO_WH_FragileStorage', DS, fx0, fx0+0.1, 0, 1.4, pz-0.06, pz+0.06)

# ---- hanging zone signs
def hang(obj, mat, cx, cz, along, y0=5.4, w=3.0, h=0.75, t=0.1):
    if along == 'z':
        m.box(obj, mat, cx-t/2, cx+t/2, y0, y0+h, cz-w/2, cz+w/2)
        for dz in (-w/2+0.3, w/2-0.3):
            m.box(obj, DS, cx-0.02, cx+0.02, y0+h, under(cx), cz+dz-0.02, cz+dz+0.02)
    else:
        m.box(obj, mat, cx-w/2, cx+w/2, y0, y0+h, cz-t/2, cz+t/2)
        for dx in (-w/2+0.3, w/2-0.3):
            m.box(obj, DS, cx+dx-0.02, cx+dx+0.02, y0+h, under(cx+dx), cz-0.02, cz+0.02)
hang('LLASO_WH_Sign_Fragile', ZF, -9.6, FZM, 'z')
hang('LLASO_WH_Sign_Cold', ZC, -9.6, CZM, 'z')
hang('LLASO_WH_Sign_Inspection', ZI, 0.0, -26.5, 'x', w=5.0)
hang('LLASO_WH_Sign_Staging', PU, 9.6, -8.5, 'z')
hang('LLASO_WH_Sign_Receiving', GR, 0.0, -2.8, 'x', y0=5.0, w=5.0)

# ------------------------------------------------------------------ materials
MTL = """newmtl Fac_White
Kd 0.96 0.97 0.98
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Grey
Kd 0.58 0.61 0.67
Ka 0 0 0
Ks 0 0 0

newmtl Fac_DarkBlue
Kd 0.05 0.12 0.33
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Purple
Kd 0.38 0.24 0.66
Ka 0 0 0
Ks 0 0 0

newmtl Fac_LightBlue
Kd 0.62 0.83 0.97
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Green
Kd 0.18 0.78 0.42
Ka 0 0 0
Ks 0 0 0

newmtl Fac_DarkSteel
Kd 0.30 0.32 0.37
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Roof
Kd 0.88 0.90 0.93
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Glass
Kd 0.72 0.88 0.97
Ka 0 0 0
Ks 0 0 0
d 0.28
Tr 0.72

newmtl Fac_Floor
Kd 0.84 0.86 0.90
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Light
Kd 1.00 1.00 1.00
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Concrete
Kd 0.46 0.48 0.53
Ka 0 0 0
Ks 0 0 0

newmtl Fac_Yellow
Kd 0.95 0.78 0.10
Ka 0 0 0
Ks 0 0 0

newmtl Zone_Fragile
Kd 0.87 0.13 0.13
Ka 0 0 0
Ks 0 0 0

newmtl Zone_Cold
Kd 0.10 0.18 0.85
Ka 0 0 0
Ks 0 0 0

newmtl Zone_Inspection
Kd 1.00 0.50 0.08
Ka 0 0 0
Ks 0 0 0
"""

import os, json
out = '/home/claude/work/out'
os.makedirs(out, exist_ok=True)
open(f'{out}/LLASO_Warehouse.mtl', 'w').write(MTL)
hdr = ("# LLASO Warehouse v1 - truck cargo receiver: 4 dock bays, inspection strip, cold + fragile storage, CTB/Rack Packing link\n"
       "# Same coordinate system as LLASO_Facility.obj: origin = centre of the hall end-face at floor level, 1 unit = 1 m.\n"
       "# Warehouse extends toward -Z (away from the facility). +X side = CTB / Rack Packing. -X side = Fragile + Cold storage.\n")
n1 = m.write(f'{out}/LLASO_Warehouse.obj', 'LLASO_Warehouse.mtl', False, hdr)
n2 = m.write(f'{out}/LLASO_Warehouse_MIRRORED.obj', 'LLASO_Warehouse.mtl', True,
             hdr + "# MIRRORED variant (X negated, winding flipped) - use with LLASO_Facility_MIRRORED.obj\n")
print(n1, n2)
json.dump({'lights': light_pos, 'arch': None}, open('/home/claude/work/lights.json', 'w'))
print('lights', len(light_pos))

# ------------------------------------------------------------------ audit
def audit(boxes, verbose=True):
    """z-fight check: two boxes of different material with a coplanar, same-facing, overlapping face."""
    eps = 1e-6; hits = []
    B = boxes
    for i in range(len(B)):
        a = B[i]
        for j in range(i + 1, len(B)):
            b = B[j]
            if a[1] == b[1]: continue
            for ax in range(3):                       # axis of face normal
                lo, hi = 2 + 2 * ax, 3 + 2 * ax
                o = [k for k in range(3) if k != ax]
                ov = 1.0
                for k in o:
                    l = max(a[2+2*k], b[2+2*k]); h = min(a[3+2*k], b[3+2*k])
                    ov *= max(0, h - l)
                if ov < 1e-4: continue
                if abs(a[hi] - b[hi]) < eps and a[lo] < b[hi] - eps and b[lo] < a[hi] - eps:
                    hits.append((a[0], a[1], b[0], b[1], 'xyz'[ax] + '+', round(a[hi], 3), round(ov, 4)))
                if abs(a[lo] - b[lo]) < eps and a[lo] < b[hi] - eps and b[lo] < a[hi] - eps:
                    hits.append((a[0], a[1], b[0], b[1], 'xyz'[ax] + '-', round(a[lo], 3), round(ov, 4)))
    if verbose:
        print('z-fight pairs:', len(hits))
        for h in hits[:60]: print('  ', h)
    return hits
if __name__ == '__main__':
    audit(m.boxes)
