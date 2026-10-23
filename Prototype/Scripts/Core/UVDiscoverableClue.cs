using UnityEngine;

// Only designated search targets use this gate; background UV traces remain
// visual context. Finding a fluorescence cue does not identify its substance.
public sealed class UVDiscoverableClue : MonoBehaviour {
 public UVTrace Trace;
 public bool Discovered {get;private set;}
 public bool CanInspect=>Discovered||(Trace&&Trace.Visibility>.08f);
 public void Remember(){if(Trace&&Trace.Visibility>.08f)Discovered=true;}
 public void ResetDiscovery(){Discovered=false;}
}
