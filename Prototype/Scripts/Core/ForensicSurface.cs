using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class ForensicSurface : MonoBehaviour {
 public SurfaceClass Class=SurfaceClass.NonPorousSmooth;
 public static SurfaceClass ClassOf(Collider c,SurfaceClass fallback=SurfaceClass.NonPorousSmooth){if(!c)return fallback;var s=c.GetComponentInParent<ForensicSurface>();return s?s.Class:fallback;}
 public static bool Porous(SurfaceClass c)=>c==SurfaceClass.Porous||c==SurfaceClass.Fabric;
}
