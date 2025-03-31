using UnityEngine;

public class ShowIfEnumAttribute : PropertyAttribute
{
    public string enumFieldName;
    public object[] enumValues;

    public ShowIfEnumAttribute(string enumFieldName, params object[] enumValues)
    {
        this.enumFieldName = enumFieldName;
        this.enumValues = enumValues;
    }
}