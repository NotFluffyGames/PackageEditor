using UnityEditor;
using UnityEngine;

namespace NotFluffy.PackageEditor
{
    public class ProgressBarHandler
    {
        public readonly string Name;
        public readonly uint Steps;
        public int CurrentStep { get; private set; }
        
        
  
       
  

        public ProgressBarHandler(string name, uint steps, string initialStepInfo)
        {
            Steps = steps;
            Name = name;
            
            MoveNext(initialStepInfo);
        }

        public void MoveNext(string info)
        {
            var progress = Mathf.Clamp01(CurrentStep++ / (float)Steps);
            EditorUtility.DisplayProgressBar(Name, info,  progress);
        }
        
    }
}