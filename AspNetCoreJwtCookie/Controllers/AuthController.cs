using AspNetCoreJwtCookie.Models;
using AspNetCoreJwtCookie.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreJwtCookie.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;

        public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration configuration )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Register([FromBody]RegisterVm model)
        {
            try
            {
                var user = new ApplicationUser() { UserName = model.UserName, Email = model.EmailAddress };
                var result = await _userManager.CreateAsync(user, model.Password);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Login([FromBody]LoginVm model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                var isValidPassword =  await _userManager.CheckPasswordAsync(user, model.Password);
                if (isValidPassword)
                {
                    var payload = GetPayload(user.UserName);
                    var token = CreateToken(payload);
                    //appent a http only cookie containing jwt
                    HttpContext.Response.Cookies.Append("access_token", token,
                        new CookieOptions() { HttpOnly = true, Expires = DateTime.Now.AddDays(1) });

                    return Ok(token);
                }
                return BadRequest("Invalid user name or password.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        private string CreateToken(JwtPayload payload)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("jwt:SecretKey").Value));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(signingCredentials);
            var jwt = new JwtSecurityToken(header, payload);
            var jwtHandler = new JwtSecurityTokenHandler();
            return jwtHandler.WriteToken(jwt);
        }

        private Claim[] GetClaims(string userName)
        {
            return new Claim[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, "iss-issuer"),
                new Claim(JwtRegisteredClaimNames.Aud, "aud-audience")
            };
        }

        private JwtPayload GetPayload(string userName)
        {
            var claims = GetClaims(userName);
            return new JwtPayload(claims);
        }
    }
}
