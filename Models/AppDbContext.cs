using Microsoft.EntityFrameworkCore;

namespace CRIPTObackend.Models
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
		public DbSet<Cliente> Clientes { get; set; }
		public DbSet<Transaccion> transacciones { get; set; }
	}
}
