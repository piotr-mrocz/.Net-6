using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using ToDos.MinimalAPI;

namespace MinimalnieAPI;
public static class ToDoRequests
{
    public static IResult GetAll(IToDoService service)
    {
        var todos = service.GetAll();
        return Results.Ok(todos);
    }

    public static IResult GetById(IToDoService service, Guid id)
    {
        var toDo = service.GetById(id);
        return toDo != null ? Results.Ok(toDo) : Results.NotFound();
    }

    [Authorize]
    public static IResult CreateToDo(IToDoService service, ToDo toDo)
    {
        service.Create(toDo);
        return Results.Created($"/todos/{toDo.Id}", toDo);
    }

    public static IResult UpdateToDo(IToDoService service, Guid id, ToDo toDo)
    {
        var toDoTask = service.GetById(id);

        if (toDoTask == null)
            return Results.NotFound();

        service.Update(toDo);
        return Results.Ok();
    }

    public static IResult DeleteToDo(IToDoService service, Guid id)
    {
        var toDoTask = service.GetById(id);

        if (toDoTask == null)
            return Results.NotFound();

        service.Delete(id);
        return Results.Ok();
    }

    // funkcja zawierająca endpointy <- wtedy wywołać ją można tylko poprzez 1 metodę
    //public static void RegisterEndpoints(WebApplication app)
    //{
    //    app.MapGet("/todos", ToDoRequests.GetAll);
    //    app.MapGet("/todos{id}", ToDoRequests.GetById);
    //    app.MapPost("/todos", ToDoRequests.CreateToDo);
    //    app.MapPut("/todos/{id}", ToDoRequests.UpdateToDo);
    //    app.MapDelete("/todos/{id}", ToDoRequests.DeleteToDo);
    //}

    public static WebApplication RegisterEndpoints(this WebApplication app)
    {
        // Poprawa zapisu w swaggerze
        app.MapGet("/todos", ToDoRequests.GetAll)
            .Produces<List<ToDo>>() // tutaj wpisujemy jakiego typu będzie zwracane dane w swaggerze + w () można wpisać jaki kod statusu chcemy zwracać, ale jest to opcjonalne
            .WithTags("Nagłówek") // opcjonalne, ale zamienia nagłówek klasy na ten, któy wpisaliśmy
            .RequireAuthorization(); // wymaga uwierzytelnienia usera w () można podać konkretne warunki, któe musi spełnić user, np. być adminem albo można nad funkcją dodać atrybut [Authorize]

        app.MapGet("/todos{id}", ToDoRequests.GetById)
            .Produces<ToDo>() // domyślnie w () jest status code 202 (OK)
            .Produces(StatusCodes.Status404NotFound) // a to zwróci w przypadku gdy niczego nie znajdzie
            .WithTags("Nagłówek")
            .AllowAnonymous(); // funkcja będzie dostępna absolutnie dla każdego = publiczna

        app.MapPost("/todos", ToDoRequests.CreateToDo)
            .Produces<ToDo>(StatusCodes.Status201Created) // status kodu jaki zwraca
            .Accepts<ToDo>("application/json") // parametr, który przyjmuje
        .WithTags("Nagłówek")
        .WithValidator<ToDo>(); // wykorzystujemy funkcję z ValidatorExtension, dzięki czemu przed dodaniem do bazy będzie sprawdzony model

        app.MapPut("/todos/{id}", ToDoRequests.UpdateToDo)
            .Produces<ToDo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Accepts<ToDo>("application/json")
            .WithTags("Nagłówek")
            .WithValidator<ToDo>(); // wykorzystujemy funkcję z ValidatorExtension, dzięki czemu przed updatem do bazy będzie sprawdzony model

        app.MapDelete("/todos/{id}", ToDoRequests.DeleteToDo)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Nagłówek")
            .ExcludeFromDescription(); // ukryje tą funkcję i nie będzie ona dostępna dla swaggera

        return app;
    }
}

