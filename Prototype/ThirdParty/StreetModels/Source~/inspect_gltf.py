import sys, trimesh, numpy as np
for p in sys.argv[1:]:
    s = trimesh.load(p, force='scene', process=False)
    print('==', p.split('/')[-1])
    for node in s.graph.nodes_geometry:
        M, g = s.graph[node]
        m = s.geometry[g].copy(); m.apply_transform(M)
        mat = getattr(m.visual, 'material', None)
        print('  %-40s tris=%6d min=%s max=%s mat=%s' % (node, len(m.faces), np.round(m.bounds[0], 2), np.round(m.bounds[1], 2), getattr(mat, 'name', None)))
