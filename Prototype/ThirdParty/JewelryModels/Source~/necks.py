import numpy as np
import items as I
from meshlib import unit, TAU
from rings import MM

def pearl_draped():
    P = I.drape_path(0.45, lift=3.6 * MM)
    P = np.roll(P, len(P) // 2, axis=0)      # start (clasp) at the back of the neck
    # the clasp (3 x 3.4 mm) would hang 2 mm off the neck at the strand centre line: let it rest on the velvet
    y = P[0, 1]; _, zc = I.bust_radius(y, np.arctan2(P[0, 0], P[0, 2] - 0))
    inward = -unit(np.array([P[0, 0], 0.0, P[0, 2] - zc]))
    return I.pearl_strand(P, closed=True, d=7.0 * MM, clasp_offset=inward * 1.8 * MM)

def pearl_dropped():
    P = I.dropped_path(0.45, seed=4) + np.array([0, 3.5 * MM, 0])
    return I.pearl_strand(P, closed=False, d=7.0 * MM)

def pendant_draped():
    P = I.drape_path(0.45, lift=0.9 * MM)
    i = np.argmax(P[:, 2])            # front-most point, the pendant hangs here
    front = unit(np.array([P[i, 0], 0.35, P[i, 2]]))
    down = unit(np.cross(front, [1, 0, 0])) * -1
    down = np.array([0, -1.0, 0]) - front * np.dot([0, -1.0, 0], front); down = unit(down)
    return I.necklace_pendant(P, True, pend_at=(P[i] + front * 1.2 * MM, down, front))

def pendant_dropped():
    P = I.dropped_path(0.45, seed=9) + np.array([0, 0.7 * MM, 0])
    i = len(P) // 2
    return I.necklace_pendant(P, False, pend_at=(P[i] + np.array([0, 2.6 * MM, 0]), np.array([0, 0, 1.0]), np.array([0, 1.0, 0])))
