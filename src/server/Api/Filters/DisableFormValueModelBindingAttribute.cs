using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Filters;

// Keeps MVC from draining the multipart body before the action's MultipartReader gets to it.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        IList<IValueProviderFactory> factories = context.ValueProviderFactories;
        for (int i = factories.Count - 1; i >= 0; i--)
        {
            if (factories[i] is FormValueProviderFactory or JQueryFormValueProviderFactory)
            {
                factories.RemoveAt(i);
            }
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
