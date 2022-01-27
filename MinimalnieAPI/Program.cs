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
                Encoding.UTF8.GetBytes(builder.Configuration["JwtKey"])) // przekazujemy kolekcj� bajt�w z warto�ci naszego klucza prywatnego
        };
    });

builder.Services.AddAuthorization(); // autoryzacja user�w

var app = builder.Build();

// Trzeba teraz wywo�a� odpowiednie metody, �eby uwierzytelnianie i athoryzacja dzia�a�y
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// funkcje, kt�re wpisali�my podczas przegl�dania kursu na youtube
#region nauka
// wersja 1
// pobranie wszystkich pozycji
app.MapGet("/todos", (IToDoService service) => service.GetAll()); // bezpo�rednio mo�na wstrzykn�� service tylko trzeba zarejestrowa� service w kontenerze DI -> przed wywo�aniem dunkcji builder.Build(). Patrz wy�ej

// pobranie konkretnej pozycji
app.MapGet("/todos/{id}", (IToDoService service, Guid id) => service.GetById(id));
// pierwszy parametr (service) dostaniemy z kontenera DI
// drugi parametr (id) dostaniemy ze �cie�ki zapytania

// U�ycie atrybut�w jest opcjonalne 

// mo�emy jeszcze jawnie zdefiniowa� kt�ry parametr sk�d pochodzi:
//app.MapGet("/todos/{id}", ([FromServices]IToDoService service, [FromRoute]Guid id) => service.GetById(id));

// dodawanie nowego to do
app.MapPost("/todos", ([FromServices]IToDoService service, [FromBody]ToDo toDo) => service.Create(toDo));
// [FromBody] - pochodzi z cia�a zapytania

// edycja to do
app.MapPut("/todos/{id}", ([FromServices] IToDoService service, Guid id, ToDo toDo) => service.Update(toDo));

// usuwanie zadania
app.MapDelete("/todos/{id}", ([FromServices] IToDoService service, Guid id) => service.Delete(id));

//Jednak przy takim zapisie endpoit�w mamy 2 problemy:
//1) Nie wskazujemy jaki status kodu ma zwr�ci� funkcja
//2) Nie ma mo�liwo�ci przetestowa� w testach jednostkowych implementacji endpointu

// W celu rozwi�zania b��d�w tworzymy now� klas� ToDoRequests, kt�rej zadaniem jest enkapsulacja endpoint�w wy�ej

// U�ycie tych samych endpoint�w w lepszej wersji - a najlepiej wynie�� to do osobnej klasy np. ToDoRequests
app.MapGet("/todos", ToDoRequests.GetAll);
app.MapGet("/todos{id}", ToDoRequests.GetById);
app.MapPost("/todos", ToDoRequests.CreateToDo);
app.MapPut("/todos/{id}", ToDoRequests.UpdateToDo);
app.MapDelete("/todos/{id}", ToDoRequests.DeleteToDo);

// Wywo�anie metody RegisterEndpoints
//1 metoda:
ToDoRequests.RegisterEndpoints(app);

//2 metoda - za pomoc� Extended method (czytelniejsze):
app.RegisterEndpoints();

// funkcja, kt�ra zwraca token <- powinna by� w osobnej klasie
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

// jak uzyska� info o zalogowanym userze i jak d�ugo wa�ny jest token
app.MapGet("/loginUser", (ClaimsPrincipal user) =>
{
    var userName = user.Identity.Name;
    return $"Witaj {userName}";
});
#endregion

app.Run();

