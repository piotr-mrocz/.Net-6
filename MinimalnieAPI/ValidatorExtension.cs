using FluentValidation;

namespace MinimalnieAPI;

// zainstaować paczkę FluentValidation.DependencyInjectionExtensions
public static class ValidatorExtension
{
    public static RouteHandlerBuilder WithValidator<T>(this RouteHandlerBuilder builder) 
        where T : class
    {
        // zanim wywoła się konkretny endpoint to wykona się funkcja poniżej. W naszym przypadku jest to funkcja, która waliduje model

        builder.Add(endpointBuilder =>
        {
            var orginalDelegate = endpointBuilder.RequestDelegate; // musimy przypisać pierwotną delegatę zanim zaczniemy ją zmieniać, bo później nie będziemy w stanie wrócić do tego

            endpointBuilder.RequestDelegate = async httpContext =>
            {
                var validator = httpContext.RequestServices.GetRequiredService<IValidator<T>>();

                httpContext.Request.EnableBuffering();
                var body = await httpContext.Request.ReadFromJsonAsync<T>();

                if (body == null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Nie można zmapować body na model!");
                    return;
                }

                var validationResult = validator.Validate(body);

                if (!validationResult.IsValid)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsJsonAsync(validationResult.Errors);
                    return;
                }

                httpContext.Request.Body.Position = 0;

                await orginalDelegate(httpContext);
            };
        });

        return builder;
    }
}
