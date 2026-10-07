using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Filters;

/// <summary>
/// MVC's default model binding can read the request form while resolving action
/// parameters for a multipart/form-data request, which would drain the body before
/// this action's manual MultipartReader gets to it. This removes the value provider
/// factories responsible, as a defense-in-depth measure alongside not declaring any
/// bindable parameters on the action itself.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        IList<IValueProviderFactory> factories = context.ValueProviderFactories;
        for (var i = factories.Count - 1; i >= 0; i--)
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
