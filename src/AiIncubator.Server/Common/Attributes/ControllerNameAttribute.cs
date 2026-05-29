using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AiIncubator.Server.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public class ControllerNameAttributeConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        ControllerNameAttribute? attribute = controller.Attributes.OfType<ControllerNameAttribute>().SingleOrDefault();
        if (attribute is not null)
        {
            controller.ControllerName = attribute.Name;
        }
    }
}
