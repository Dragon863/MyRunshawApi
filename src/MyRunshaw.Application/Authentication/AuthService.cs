using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Authentication;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<string> LoginWithEntraAsync(string entraIdToken)
    {
        var tenantId = _configuration["EntraId:TenantId"];
        var clientId = _configuration["EntraId:ClientId"];

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever());

        var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            ValidAudience = clientId,
            IssuerSigningKeys = openIdConfig.SigningKeys
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(entraIdToken, validationParameters, out var validatedToken);

        var email = principal.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        var name = principal.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown User";

        if (string.IsNullOrEmpty(email) || (!email.EndsWith("@runshaw.ac.uk") && !email.EndsWith("@student.runshaw.ac.uk")))
        {
            throw new UnauthorizedAccessException("Only active Runshaw students can log in.");
        }

        var studentId = email.Split('@')[0].ToLowerInvariant();

        var user = await _userRepository.GetByStudentIdAsync(studentId);

        if (user == null)
        {
            user = new User { StudentId = studentId, Name = name };
            await _userRepository.AddAsync(user);
        }
        else if (user.Name != name)
        {
            // we should update the name if it's been changed in Entra ID
            user.Name = name;
            await _userRepository.UpdateAsync(user);
        }

        return GenerateCustomJwt(user);
    }


    public string GenerateCustomJwt(User user)
    {
        var secret = _configuration["JwtSettings:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // the Subject (sub) claim will hold the StudentId
            new Claim(JwtRegisteredClaimNames.Sub, user.StudentId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(double.Parse(_configuration["JwtSettings:ExpirationInDays"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}