using Microsoft.AspNetCore.Authentication.JwtBearer;
using Aaron.Pina.Blog.Article._12.Shared.Responses;
using Aaron.Pina.Blog.Article._12.Shared;
using Aaron.Pina.Blog.Article._12.Other;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(Configuration.JwtBearer.Options);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/user", (HttpContext context) =>
    {
        var clientId = context.User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(clientId)) return Results.BadRequest("No client id found");
        var scope = context.User.FindFirstValue("scope");
        if (string.IsNullOrEmpty(scope)) return Results.BadRequest("No scope found");
        var permissions = ScopeParser.ExtractPermissions(scope);
        return Results.Ok(new UserResponse(clientId, string.Join(',', permissions)));
    })
   .RequireAuthorization();

app.Run();
