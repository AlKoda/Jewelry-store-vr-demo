# Supplied police car

Extracted directly from Koda's `police car 3d model.zip` on October 20, 2026. The FBX and JPEG bytes are unchanged. No decimation, planar conversion, remeshing, vertex editing or nonuniform scaling is applied.

FBX SHA-256: `BEF2445C622AC69BAFB734D90BE5F7173EC9046F03790B48DD18D6761CE1445A`.

Unity uses the imported FBX mesh directly. Current user-requested presentation is plain gray with no texture maps and unsmoothed polygon shading, through the project-authored CrimeScene/RawCarClay material. Screen derivatives provide face normals without altering the FBX, splitting vertices or replacing meshes. Authored import settings/GUID remain intact; the original base-colour image and textured material are retained for later use. A uniform instance scale fits a 4.6 m car; instance rotations align its nose and park it along the road. A separate box provides collision. `PresentationPolish.ParkCar` owns this placement. The previous car pack is retained for history.

Source inspection: Blender reports 3,889 vertices, 4,176 polygons (1,035 triangles and 3,141 quads), no subdivision modifiers. Unity renders 7,317 triangles. Surface-only changes preserve every triangle position; checked signature: 310B35C73973D2651191190446DBA687F068DE308E177FB9E56A4DCC221E3E29.
