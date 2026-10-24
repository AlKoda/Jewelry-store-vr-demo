using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// What a surface is made of decides the method that works on it (HOSDB Fingerprint Powders
// Guidelines 2007; UK Fingermark Visualisation Manual): see Docs/Research/FORENSIC_MECHANICS_SPEC.md.
public enum SurfaceClass { NonPorousSmooth, NonPorousTextured, Steel, Porous, Soil, Dusty, Fabric }

// How a recorded step compares with practice. Nothing is blocked by a flag: the sandbox records
// what was done, and the review (register JSON, later a review screen) judges it.
public enum StepFlag { Correct, Acceptable, OutOfSequence, WrongMethod, Discipline }

[Serializable]
public sealed class ProcedureStep { public string step, tool, utc, note; public StepFlag flag; }

// The ordered record of what was done to one piece of evidence: searched, photographed (with or
// without a scale), developed, lifted, swabbed, tested, cast, collected, and the slips along the
// way (lift before photograph, powder on paper, mark trodden on). One per EvidenceClue, added on
// first use; cleared by the session reset.


// Marks a scene object (a glass top, a painted frame, a paper) with its surface class, so a
// powder brush swept over anything knows whether powder is the right method there.


// The rules of thumb the log applies, each traced to the research notes.
public static class ForensicRules {
 // Powder applicator against surface (fingerprints.md §3, HOSDB 2007): glass-fibre brush with
 // aluminium or black granular powder on glass and smooth surfaces; magnetic powder on textured
 // plastic, never on steel (it is drawn to the metal); no powder at all on porous paper.
 public static StepFlag Applicator(FieldInvestigationTool.Method method,SurfaceClass surface){
  if(ForensicSurface.Porous(surface))return StepFlag.WrongMethod;
  if(method==FieldInvestigationTool.Method.MagneticWand)return surface==SurfaceClass.Steel?StepFlag.WrongMethod:surface==SurfaceClass.NonPorousTextured?StepFlag.Correct:StepFlag.Acceptable;
  return surface==SurfaceClass.NonPorousTextured?StepFlag.Acceptable:StepFlag.Correct;
 }
 // Packaging by method and material (biological_trace.md §8: paper for anything biological so
 // it can dry; rigid containers for casts and sharp items; card lifts in envelopes).
 public static string Container(FieldInvestigationTool.Method method,bool biological){
  switch(method){
   case FieldInvestigationTool.Method.Swab:return "swab box, paper envelope";
   case FieldInvestigationTool.Method.Tape:case FieldInvestigationTool.Method.LiftCard:return "lift on backing card, envelope";
   case FieldInvestigationTool.Method.GelLifter:return "gel lifter with cover sheet, rigid envelope";
   case FieldInvestigationTool.Method.CastingKit:return "cast in cardboard box, marked on the back";
   case FieldInvestigationTool.Method.MikrosilKit:return "silicone cast in a labelled box";
   default:return biological?"paper bag or paper fold, sealed":"tamper-evident bag, sealed";
  }
 }
 public static string MethodName(FieldInvestigationTool.Method method){
  switch(method){
   case FieldInvestigationTool.Method.Forceps:return "forceps";case FieldInvestigationTool.Method.Swab:return "swab";
   case FieldInvestigationTool.Method.Brush:return "powder brush";case FieldInvestigationTool.Method.LiftCard:return "hinge lifter";
   case FieldInvestigationTool.Method.MagneticWand:return "magnetic wand";case FieldInvestigationTool.Method.Tape:return "lifting tape";
   case FieldInvestigationTool.Method.GelLifter:return "gel lifter";case FieldInvestigationTool.Method.KMTest:return "presumptive blood test";
   case FieldInvestigationTool.Method.CastingKit:return "dental-stone casting kit";case FieldInvestigationTool.Method.MikrosilKit:return "silicone casting kit";
   default:return method.ToString();
  }
 }
}
