namespace FoodTour.Services;

public interface IRazorTemplateRenderer {
    Task<string> RenderTemplateAsync<TModel>(string templatePath, TModel model);
}
