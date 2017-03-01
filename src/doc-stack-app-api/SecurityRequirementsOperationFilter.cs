using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using System;
using IdentityServer4;

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(Operation operation, OperationFilterContext context)
    {
        // Policy names map to scopes
        var controllerScopes = context.ApiDescription.ControllerAttributes()
            .OfType<AuthorizeAttribute>()
            .Select(attr => attr.Policy);

        var actionScopes = context.ApiDescription.ActionAttributes()
            .OfType<AuthorizeAttribute>()
            .Select(attr => attr.Policy);

        var requiredScopes = controllerScopes.Union(actionScopes).Distinct().ToList();
        requiredScopes.Add("doc-stack-app-api");
        requiredScopes.Add(IdentityServerConstants.StandardScopes.Profile);
        requiredScopes.Add(IdentityServerConstants.StandardScopes.OpenId);

        if (requiredScopes.Any())
        {
            operation.Responses.Add("401", new Response { Description = "Unauthorized" });
            operation.Responses.Add("403", new Response { Description = "Forbidden" });

            operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
            operation.Security.Add(new Dictionary<string, IEnumerable<string>>
            {
                { "oauth2", requiredScopes }
            });
        }

    }
}
