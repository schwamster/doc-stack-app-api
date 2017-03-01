

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

[Route("identity")]
[Authorize]
public class IdentityController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var claims = User.Claims.ToList();

        var token = await this.HttpContext.Authentication.GetTokenAsync("access_token");
        claims.Add(new System.Security.Claims.Claim("token", token));
        return new JsonResult(from c in claims select new { c.Type, c.Value });
    }
}