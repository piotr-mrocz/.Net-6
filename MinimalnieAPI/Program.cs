using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MinimalnieAPI;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ToDos.MinimalAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IToDoService, ToDoService>();
builder.Services.AddValidatorsFromAssemblyContaining(typeof(ToDoValidator)); // zarejestrowanie serwisu validatora

//paczka do zainstalowania Microsoft.AspNetCore.Authentication.JwtBearer
builder.Services.AddAuthentication("Bearer") // konfiguracja uwierzytelniania
    .AddJwtBearer(config =>
    {
        config.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["JwtIssuer"], // dane z pliku appsettings.json
            ValidAudience = builder.Configuration["JwtIssuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtKey"])) // przekazujemy kolekcjê bajtów z wartoœci naszego klucza prywatnego
        };
    });

builder.Services.AddAuthorization(); // autoryzacja userów

var app = builder.Build();

// Trzeba teraz wywo³aæ odpowiednie metody, ¿eby uwierzytelnianie i athoryzacja dzia³a³y
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// funkcje, które wpisaliœmy podczas przegl¹dania kursu na youtube
#region nauka
// wersja 1
// pobranie wszystkich pozycji
app.MapGet("/todos", (IToDoService service) => service.GetAll()); // bezpoœrednio mo¿na wstrzykn¹æ service tylko trzeba zarejestrowaæ service w kontenerze DI -> przed wywo³aniem dunkcji builder.Build(). Patrz wy¿ej

// pobranie konkretnej pozycji
app.MapGet("/todos/{id}", (IToDoService service, Guid id) => service.GetById(id));
// pierwszy parametr (service) dostaniemy z kontenera DI
// drugi parametr (id) dostaniemy ze œcie¿ki zapytania

// U¿ycie atrybutów jest opcjonalne 

// mo¿emy jeszcze jawnie zdefiniowaæ który parametr sk¹d pochodzi:
//app.MapGet("/todos/{id}", ([FromServices]IToDoService service, [FromRoute]Guid id) => service.GetById(id));

// dodawanie nowego to do
app.MapPost("/todos", ([FromServices]IToDoService service, [FromBody]ToDo toDo) => service.Create(toDo));
// [FromBody] - pochodzi z cia³a zapytania

// edycja to do
app.MapPut("/todos/{id}", ([FromServices] IToDoService service, Guid id, ToDo toDo) => service.Update(toDo));

// usuwanie zadania
app.MapDelete("/todos/{id}", ([FromServices] IToDoService service, Guid id) => service.Delete(id));

//Jednak przy takim zapisie endpoitów mamy 2 problemy:
//1) Nie wskazujemy jaki status kodu ma zwróciæ funkcja
//2) Nie ma mo¿liwoœci przetestowaæ w testach jednostkowych implementacji endpointu

// W celu rozwi¹zania b³êdów tworzymy now¹ klasê ToDoRequests, której zadaniem jest enkapsulacja endpointów wy¿ej

// U¿ycie tych samych endpointów w lepszej wersji - a najlepiej wynieœæ to do osobnej klasy np. ToDoRequests
app.MapGet("/todos", ToDoRequests.GetAll);
app.MapGet("/todos{id}", ToDoRequests.GetById);
app.MapPost("/todos", ToDoRequests.CreateToDo);
app.MapPut("/todos/{id}", ToDoRequests.UpdateToDo);
app.MapDelete("/todos/{id}", ToDoRequests.DeleteToDo);

// Wywo³anie metody RegisterEndpoints
//1 metoda:
ToDoRequests.RegisterEndpoints(app);

//2 metoda - za pomoc¹ Extended method (czytelniejsze):
app.RegisterEndpoints();

// funkcja, która zwraca token <- powinna byæ w osobnej klasie
app.MapGet("/token", () =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "user-id"),
        new Claim(ClaimTypes.Name, "Test Name"),
        new Claim(ClaimTypes.Role, "Admin"),
    };

    var token = new JwtSecurityToken
    (
        issuer: builder.Configuration["JwtIssuer"],
        audience: builder.Configuration["JwtIssuer"],
        claims: claims,
        expires: DateTime.UtcNow.AddDays(60),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtKey"])),
            SecurityAlgorithms.HmacSha256)
    );

    var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
    return jwtToken;
});

// jak uzyskaæ info o zalogowanym userze i jak d³ugo wa¿ny jest token
app.MapGet("/loginUser", (ClaimsPrincipal user) =>
{
    var userName = user.Identity.Name;
    return $"Witaj {userName}";
});
#endregion

app.Run();

