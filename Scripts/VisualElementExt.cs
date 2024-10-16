using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace NotFluffy.PackageEditor
{
    public static class VisualElementExt
    {
        public static VisualElement GetRoot(this VisualElement element)
        {
            while (element is { parent: not null })
                element = element.parent;

            return element;
        }
    }
}
#endif