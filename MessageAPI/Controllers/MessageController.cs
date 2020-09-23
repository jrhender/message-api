using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MessageAPI.Models;
using MessageAPI.Infrastructure;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MessageAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly MessageDbContext _dbContext;

        public MessageController(MessageDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        /// <summary>
        /// GET (Read all) /message
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerator<Message>>> GetAll()
        {
            var messages = await _dbContext.Messages.ToArrayAsync();
            return Ok(messages);
        }

        /// <summary>
        /// GET (Read) /message/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Message>> Get(int id)
        {
            var message = await _dbContext.Messages.FindAsync(id);

            if (message == null)
                return NotFound();

            return Ok(message);
        }

        /// <summary>
        /// POST (Create) /message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Message>> Create([FromBody] Message message)
        {
            if (string.IsNullOrEmpty(message.Content))
                return BadRequest();

            var existingMessage = await _dbContext.Messages.FindAsync(message.Id);
            if (existingMessage != null)
                return Conflict();

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new {id = message.Id}, message);
        }

        /// <summary>
        /// DELETE /message/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Message>> Delete(int id)
        {
            var message = await _dbContext.Messages.FindAsync(id);
            if (message == null)
                return NotFound();

            _dbContext.Messages.Remove(message);
            await _dbContext.SaveChangesAsync();
            return Ok(message);
        }

        /// <summary>
        /// PUT (Update) a Message by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Message message)
        {
            if (message.Id != id || string.IsNullOrEmpty(message.Content))
                return BadRequest();

            var messageOld = await _dbContext.Messages.FindAsync(id);
            if (messageOld == null)
                return NotFound();
            messageOld = message;
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}