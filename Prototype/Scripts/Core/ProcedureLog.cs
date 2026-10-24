using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class ProcedureLog : MonoBehaviour {
 public readonly List<ProcedureStep> Steps=new List<ProcedureStep>();
 public int Flags {get;private set;}
 public static ProcedureLog Of(Component c){if(!c)return null;var log=c.GetComponent<ProcedureLog>();return log?log:c.gameObject.AddComponent<ProcedureLog>();}
 public ProcedureStep Add(string step,string tool,StepFlag flag,string note=null){
  var s=new ProcedureStep{step=step,tool=tool,utc=DateTime.UtcNow.ToString("o"),flag=flag,note=note};Steps.Add(s);
  if(flag==StepFlag.OutOfSequence||flag==StepFlag.WrongMethod||flag==StepFlag.Discipline)Flags++;return s;
 }
 public bool Has(string step){foreach(var s in Steps)if(s.step==step)return true;return false;}
 public ProcedureStep Last(string step){for(int i=Steps.Count-1;i>=0;i--)if(Steps[i].step==step)return Steps[i];return null;}
 public void Clear(){Steps.Clear();Flags=0;}
 // One line per step, for the register entry and the JSON record.
 public string Summary(){var b=new StringBuilder();foreach(var s in Steps){if(b.Length>0)b.Append("; ");b.Append(s.step);if(!string.IsNullOrEmpty(s.tool))b.Append(" (").Append(s.tool).Append(')');if(s.flag!=StepFlag.Correct)b.Append(" [").Append(s.flag).Append(']');}return b.ToString();}
}
