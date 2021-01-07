using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Feature.Auth.Rendering.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IConfiguration configuration;

        public AuthController(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            this.clientFactory = clientFactory;
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("/auth/login", Name = "Login")]
        public async Task<IActionResult> Login(Models.Login login)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{configuration["Sitecore:InstanceUri"]}/sitecore/api/ssc/auth/login");
            request.Content = new StringContent($"{{\"domain\":\"extranet\",\"username\":\"{login.Username}\",\"password\":\"{login.Password}\"}}", Encoding.UTF8, "application/json");
            HttpClient client = clientFactory.CreateClient();
            HttpResponseMessage response = await client.SendAsync(request);          

            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TryGetValues("Set-Cookie", out var newCookies))
                {
                    foreach (SetCookieHeaderValue item in SetCookieHeaderValue.ParseList(newCookies.ToList()))
                    {
                        if (item.Name.Value == ".AspNet.Cookies")
                        {
                            var cookieValue = $"{item.Value.Value}; expires={item.Expires.Value}; path={item.Path.Value}; secure; HttpOnly; SameSite={item.SameSite}";
                            Response.Cookies.Append(item.Name.Value, cookieValue);
                        }
                    }
                }
                return Redirect("/members/home");
            }
            else
            {
                login.Password = string.Empty;
                login.IsValid = false;
                login.Message = "Login failed, please try again.";
                return Redirect("/login");
            }
        }
    }
}