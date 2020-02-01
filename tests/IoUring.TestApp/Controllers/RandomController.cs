﻿using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace IoUring.TestApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RandomController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly Task<string> t = Task.FromResult("Hello");

        public RandomController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public Task<string> Get()
        {
            return t;
        }
    }
}