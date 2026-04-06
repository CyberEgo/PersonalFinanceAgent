using System.Reflection;

namespace PersonalFinance.AgentBackend;

public static class WorkflowExtensions
{
    public static T SetName<T>(this T workflow, string name) where T : class
    {
        var type = workflow.GetType();
        var nameProperty = type.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);

        if (nameProperty != null)
        {
            var backingField = type.GetField("<Name>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (backingField != null)
            {
                backingField.SetValue(workflow, name);
            }
            else if (nameProperty.CanWrite)
            {
                nameProperty.SetValue(workflow, name);
            }
        }

        return workflow;
    }
}
