using UnityEditor;

namespace NotFluffy.PackageEditor
{
    public class ProgressBarHandler
    {
        public readonly string Name;
        public readonly uint Steps;
        public int CurrentStep { get; private set; } = -1;

        public ProgressBarHandler(string name, uint steps)
        {
            Steps = steps;
            Name = name;
        }

        public void MoveNext(string info)
        {
            EditorUtility.DisplayProgressBar(Name, info, ++CurrentStep / (float)Steps);
        }
        
    }
}