﻿using System;
using System.Threading.Tasks;
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
        public async Task<IActionResult> Get()
        {
            var key = await _keyService.GetAKeyAsync();

            if (key == null)
            {
                return Ok("Rate limit reached for today");
            }

            return Ok(key);
        }
        
        [HttpGet("keys/{count}")]
        public async Task<IActionResult> Get(int count)
        {
            var keys = await _keyService.GetKeysAsync(count);
            
            if (keys == null)
            {
                return Ok("Rate limit reached for today");
            }
            
            return Ok(keys);
        }
        
        [HttpPost("ReturnKeys")]
        public async Task<IActionResult> Post([FromBody] ReturnKeyDto returnKeyDto)
        {
            await _keyService.ReturnKeysAsync(returnKeyDto);
            
            return Ok();
        }
    }
}