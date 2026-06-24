using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.ServiceInterfaces;

public class TokenService : ITokenService
{
    readonly string _key;
    readonly string _issuer;
    readonly string _duration;
    public TokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"] ?? "This is the alternate key";
        _issuer = configuration["Jwt:Issuer"] ?? "Any Server";
        _duration = configuration["Jwt:ExpiryInHours"] ?? "8";
    }
    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
                issuer: _issuer,
                claims: claims,
                expires: DateTime.Now.AddHours(Convert.ToDouble(_duration)),
                signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string CreateCustomerToken(CustomerTokenRequest request)
    {
        var claims = new List<Claim>
        {
            new("SessionId", request.SessionId.ToString()),

            new("TableId",request.TableId.ToString()),

            new("CartId",request.CartId.ToString()),

            new("WaiterId",request.WaiterId.ToString()),

            new(ClaimTypes.Role,"Customer")
        };
        return GenerateToken(claims);
    }
    public string CreateEmployeeToken(EmployeeTokenRequest request)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new(ClaimTypes.Name,request.Username),
            new(ClaimTypes.Role,request.Role)
        };
        return GenerateToken(claims);
    }
}