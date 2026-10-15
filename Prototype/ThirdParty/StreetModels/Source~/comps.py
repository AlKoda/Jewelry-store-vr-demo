import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components

def components(vertices, faces, decimals=6):
    """Face labels of position-connected components (vertices welded by position)."""
    key = np.round(vertices, decimals)
    _, inv = np.unique(key, axis=0, return_inverse=True)
    inv = inv.reshape(-1)
    f = inv[faces]
    n = inv.max() + 1
    r = np.concatenate([f[:, 0], f[:, 1], f[:, 2]])
    c = np.concatenate([f[:, 1], f[:, 2], f[:, 0]])
    g = coo_matrix((np.ones(len(r)), (r, c)), shape=(n, n))
    k, lab = connected_components(g, directed=False)
    return k, lab[f[:, 0]]
