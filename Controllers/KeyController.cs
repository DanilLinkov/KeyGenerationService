using System;
using System.Threading.Tasks;
using KeyGenerationService.Controllers.QueryParameters;
using KeyGenerationService.Dtos;
using KeyGenerationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeyGenerationService.Controllers
{
    [Authorize]
    [Controller]
    [Route("api/")]
    public class KeyController : ControllerBase
    {
        private readonly IKeyService _keyService;

        public KeyController(IKeyService keyService)
        {
            _keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
        }

        [HttpGet("key")]
        public async Task<IActionResult> Get([FromQuery] GetKeyQueryParameter parameter)
        {
            SetParameterBetweenLimits(parameter);

            var key = await _keyService.GetAKeyAsync(parameter.size);

            if (key == null)
            {
                return Ok("Rate limit reached for today");
            }

            return Ok(key);
        }

        [HttpGet("keys/{count}")]
        public async Task<IActionResult> Get(int count, [FromQuery] GetKeyQueryParameter parameter)
        {
            SetParameterBetweenLimits(parameter);

            var keys = await _keyService.GetKeysAsync(count, parameter.size);
            
            return keys == null ? Ok("Rate limit reached for today") : Ok(keys);
        }
        
        private static void SetParameterBetweenLimits(GetKeyQueryParameter parameter)
        {
            if (parameter.size <= 3)
            {
                parameter.size = 8;
            }

            if (parameter.size > 16)
            {
                parameter.size = 16;
            }
        }
        
        [HttpPost("ReturnKeys")]
        public async Task<IActionResult> Post([FromBody] ReturnKeyDto returnKeyDto)
        {
            await _keyService.ReturnKeysAsync(returnKeyDto);
            
            return Ok();
        }
    }
}