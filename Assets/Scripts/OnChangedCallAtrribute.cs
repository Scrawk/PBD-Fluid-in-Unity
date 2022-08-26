/**
 * Taken from GitHub Gist user: Bahamutho (https://gist.github.com/Bahamutho)
 * https://gist.github.com/Bahamutho/23cf3312aae627ad512e3e1825c9a1ed
 */
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

public class OnChangedCallAttribute : PropertyAttribute
{
    public enum RunTimeCriteriaEnum
    {
        EditorOnly,
        PlayModeOnly,
        Both
    }
    public string methodName;
    public object[] arguments;
    public RunTimeCriteriaEnum Criteria = RunTimeCriteriaEnum.Both;
    
    public OnChangedCallAttribute(string methodNameNoArguments, RunTimeCriteriaEnum runTimeCriteria = RunTimeCriteriaEnum.Both)
    {
        methodName = methodNameNoArguments;
        arguments = new object[0];
        Criteria = runTimeCriteria;
    }

    public OnChangedCallAttribute(string methodNameNoArguments, object[] arguments, RunTimeCriteriaEnum runTimeCriteria = RunTimeCriteriaEnum.Both)
    {
        methodName = methodNameNoArguments;
        this.arguments = arguments;
        Criteria = runTimeCriteria;
    }
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(OnChangedCallAttribute))]
public class OnChangedCallAttributePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, label);
        if (EditorGUI.EndChangeCheck())
        {
            OnChangedCallAttribute at = attribute as OnChangedCallAttribute;
            //Exit if it is not in the right play mode
            if (at.Criteria != OnChangedCallAttribute.RunTimeCriteriaEnum.Both
                 && ((!EditorApplication.isPlaying && at.Criteria == OnChangedCallAttribute.RunTimeCriteriaEnum.PlayModeOnly)
                 || (EditorApplication.isPlaying && at.Criteria == OnChangedCallAttribute.RunTimeCriteriaEnum.EditorOnly)))
                return;

            Type parentType = property.serializedObject.targetObject.GetType();

            var findMethod = parentType.GetMethods().Where(m => m.Name == at.methodName).ToList();
            if(findMethod.Count() == 0) // Found?
            {
                Debug.LogError(string.Format("Error: [OnChangedCall(\"{0}\")] Method Name in ({1}) not found. Did you perhaps typo?", at.methodName, parentType.ToString()));
                return;
            }

            var method = findMethod.First();
            if(method.GetParameters().Length != at.arguments.Length) // All arguments supplied?
            {
                Debug.LogError(string.Format("Error: [OnChangedCall] {0} in ({1}) Requires {2} arguments {3} supplied.", at.methodName, parentType.ToString(), method.GetParameters().Length, at.arguments.Length));
                return;
            }

            method.Invoke(property.serializedObject.targetObject, at.arguments);
        }
    }
}

#endif