# Local investigation detail candidates

October 23: FieldDocuments/ is active original Arabic paper/label artwork for the carryable exterior field kit, register and office box. Reproduce with Tools/make_field_documents.py; seven per-file hashes in FieldDocuments/sources.json. Case geometry is authored in Editor/NightStore/PortableFieldwork.cs; existing project-created ForensicModels brush/powder jar and SurfaceDecals ridge atlas are reused with original notices retained. RepairIntake is the physical Arabic repair-box sheet. Earlier car candidates below remain unused and car work stays paused.

Status: car work paused at Koda's request. The existing scene police car remains
plain gray with RawCarClay. Native livery files below are unapplied drafts.
The additional CC0 sedan was rejected after in-scene review (shape/finish below
the project's target); neither civilian-car instance remains in the scene.
Sources are retained for later evaluation rather than deleted.

Tools/paint_police_livery.py paints navy/white colours from exported root-local
triangles (PoliceLiveryInspectRun). It never changes the supplied FBX, UVs,
normals or importer. PoliceAuthoredPaint is actual clean albedo;
PoliceSurfaceResponse packs metallic in R and polish in G. The original
distorted text and baked reflections are not rendered. Both doors carry
correctly oriented POLICE, the official badge and الشرطة; both blue plates
contain only الشرطة, no digits. OmanPoliceCar evaluates geometry normals
including SH, preserving the requested faceted finish. No emission or extra
car geometry. Native layout generation was used instead of image generation.

The official badge is the left part of the genuinely transparent ROP header.
Copyright ROP: permission for public redistribution is unresolved. This
preview stays local, without GitHub or official-archive promotion.

CC0 Innerscene sedan candidate is combined into ten material groups. Three
LODs: 14,697 / 8,000 / 7,023 triangles. It is not used by the delivered scene.

Source original retained at .asset-cache/graphite-sedan.glb. Conversion uses
Tools/glb_to_obj.py, followed by per-material merging and simplification.
