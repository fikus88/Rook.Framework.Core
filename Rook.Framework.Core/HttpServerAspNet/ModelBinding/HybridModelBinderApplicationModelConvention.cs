using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Reflection;

namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public class HybridModelBinderApplicationModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    if (action.Parameters.Count() == 1)
                    {
                        var parameterModel = action.Parameters.First();
                        var parameterType = parameterModel.ParameterInfo.ParameterType;
                        var hasBindingAttribute = parameterModel.Attributes
                            .Where(x => typeof(IBindingSourceMetadata).IsAssignableFrom(x.GetType()))
                            .Any();

                        if (!hasBindingAttribute &&
                            parameterType.IsClass &&
                            !parameterType.IsAbstract &&
                            parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Count() > 0 &&
                            parameterType != typeof(string))
                        {
                            parameterModel.BindingInfo = new BindingInfo()
                            {
                                BindingSource = new HybridBindingSource()
                            };
                        }
                    }
                }
            }
        }
    }
}