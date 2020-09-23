using System;
using Microsoft.EntityFrameworkCore;
using MessageAPI.Models;

namespace MessageAPI.Infrastructure
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options) { }

        public DbSet<Message> Messages { get; set; }
    }
}
