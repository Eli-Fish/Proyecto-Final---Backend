using CRIPTObackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRIPTObackend.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ClienteController : Controller
	{
		private readonly AppDbContext _context;

		public ClienteController(AppDbContext context)
		{
			_context = context;
		}

		[HttpPost]
		public IActionResult Post([FromBody] Cliente cliente)
		{
			if (cliente == null)
				return BadRequest("No se enviaron datos");

			_context.Clientes.Add(cliente);
			_context.SaveChanges(); //

			return Ok(new { mensaje = "Cliente creado correctamente" });
		}

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			var clientes = await _context.Clientes.ToListAsync();
			return Ok(clientes);
		}
	}
}
