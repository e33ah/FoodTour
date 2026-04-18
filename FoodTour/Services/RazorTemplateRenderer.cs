namespace FoodTour.Services;

public class RazorTemplateRenderer(
    IRazorViewEngine razorViewEngine,
    ITempDataProvider tempDataProvider,
    IServiceProvider serviceProvider) : IRazorTemplateRenderer {

    // Renders a Razor template to a string using the specified model.
    public async Task<string> RenderTemplateAsync<TModel>(string templatePath, TModel model) {
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = serviceProvider },
            new RouteData(),
            new ActionDescriptor()
        );

        var viewResult = razorViewEngine.FindView(actionContext, templatePath, false);

        if (!viewResult.Success) {
            throw new InvalidOperationException($"Could not find view '{templatePath}'. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? [])}");
        }

        await using var stringWriter = new StringWriter();
        var viewDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary()) {
            Model = model
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            stringWriter,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return stringWriter.ToString();
    }
}
