﻿using KeycloakTesting.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ✅ Swagger конфигурация — ДО builder.Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Аутентификация через Keycloak
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:8080/realms/myrealm"; // Keycloak realm
        options.Audience = "project"; // client_id
        options.RequireHttpsMetadata = false; // only for local/dev

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "role", // this is important
            NameClaimType = "preferred_username"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

                var resourceAccess = context.Principal.FindFirst("resource_access")?.Value;
                if (resourceAccess != null)
                {
                    var parsed = JObject.Parse(resourceAccess);
                    var projectRoles = parsed["project"]?["roles"];
                    if (projectRoles != null)
                    {
                        foreach (var role in projectRoles)
                        {
                            claimsIdentity.AddClaim(new Claim("role", role.ToString()));
                        }
                    }
                }

                return Task.CompletedTask;
            }
        };
    });



builder.Services.AddAuthorization();
builder.Services.AddHttpClient<UserController>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    // Add JWT Bearer token support
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\nExample: Bearer abc123"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

// ✅ Подключаем Swagger в Dev-режиме
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
