using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace IoUring.TestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RandomController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public RandomController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            using var client = _clientFactory.CreateClient();
            var result = await client.GetAsync("https://api.chucknorris.io/jokes/random");
            return await result.Content.ReadAsStringAsync();
        }
    }
}