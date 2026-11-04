using Ecommerce.Web.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Ecommerce.Web.Filters;

// Centralise la traduction des erreurs venant des services distants (via la Gateway) en
// réponses lisibles, pour éviter de répéter le même try/catch dans chaque action.
public sealed class ApiExceptionFilter(ITempDataDictionaryFactory tempDataDictionaryFactory) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case HttpRequestException:
            case TaskCanceledException:
                context.Result = new ViewResult { ViewName = "ServiceUnavailable" };
                context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.ExceptionHandled = true;
                break;

            case OrderingApiException orderingApiException:
                var tempData = tempDataDictionaryFactory.GetTempData(context.HttpContext);
                tempData["Error"] = orderingApiException.Message;
                context.Result = new RedirectToActionResult("Index", "Cart", null);
                context.ExceptionHandled = true;
                break;
        }
    }
}
