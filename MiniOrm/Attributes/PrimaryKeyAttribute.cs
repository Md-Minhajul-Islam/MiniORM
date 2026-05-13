namespace MiniOrm.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class PrimaryKeyAttribute : Attribute
{
    public string Name {get;}
    public PrimaryKeyAttribute(string name)
    {
        Name = name;
    }
}

