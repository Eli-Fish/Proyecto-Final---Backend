using CRIPTObackend.DTOs;
using CRIPTObackend.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace CRIPTObackend.Controllers
{

	[Route("transacciones")]
	[ApiController]
	public class TransaccionesController : Controller
	{
		private readonly AppDbContext _context;

		public TransaccionesController(AppDbContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> CrearTransaccion([FromBody] TransaccionDTO transaccionDto)
		{
			if (transaccionDto.CantidadCripto <= 0)
				return BadRequest("La cantidad debe ser mayor que 0.");

			if (transaccionDto.Fecha.Date != DateTime.Today)
				return BadRequest("La fecha de la transacción debe ser el día de hoy.");

			if (transaccionDto.Tipo.ToLower() == "venta")
			{
				decimal totalCompras = await _context.transacciones.Where
					(trans => trans.ClienteId == transaccionDto.ClienteId && trans.CodigoCripto == transaccionDto.CodigoCripto && trans.Tipo == "compra")
					.SumAsync(trans => trans.CantidadCripto);

				decimal totalVentas = await _context.transacciones.Where
					(trans => trans.ClienteId == transaccionDto.ClienteId && trans.CodigoCripto == transaccionDto.CodigoCripto && trans.Tipo == "venta")
					.SumAsync(trans => trans.CantidadCripto);

				if (transaccionDto.CantidadCripto > (totalCompras - totalVentas))
					return BadRequest("No se puede vender más de lo que posee el cliente.");
			}

			var http = new HttpClient();
			string url = $"https://criptoya.com/api/{transaccionDto.CodigoCripto}/ars/1";
			var response = await http.GetStringAsync(url);
			var json = JObject.Parse(response);

			Console.WriteLine(json);

			decimal mejorPrecio = decimal.MaxValue;

			foreach (var exchange in json)
			{
				var datos = exchange.Value;

				// Ignorar nodos que no contienen precios
				if (datos["totalAsk"] == null && datos["totalBid"] == null) continue;

				decimal ask = 0, bid = 0;

				decimal.TryParse(datos["totalAsk"]?.ToString() ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out ask);
				decimal.TryParse(datos["totalBid"]?.ToString() ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out bid);

				decimal precioExchange = Math.Min(ask, bid);

				if (precioExchange > 0) mejorPrecio = Math.Min(mejorPrecio, precioExchange);
			}

			if (mejorPrecio == 0) return BadRequest("no hay precio disponible para esta criptomoneda en este momento.");
			
			decimal montoTotal = mejorPrecio * transaccionDto.CantidadCripto;

			Transaccion transaccion = new Transaccion
			{
				CodigoCripto = transaccionDto.CodigoCripto,
				Tipo = transaccionDto.Tipo.ToLower(), 
				ClienteId = transaccionDto.ClienteId,
				CantidadCripto = transaccionDto.CantidadCripto,
				Fecha = transaccionDto.Fecha,
				Monto = montoTotal
			};

			_context.transacciones.Add(transaccion);
			await _context.SaveChangesAsync();

			return Ok($"La {transaccionDto.Tipo} fue registrada correctamente.");
		}

		[HttpGet]
		public async Task<IActionResult> ObtenerTransacciones([FromQuery] int? clienteId)
		{
			var query = _context.transacciones.AsQueryable();

			if (clienteId.HasValue)
				query = query.Where(trans => trans.ClienteId == clienteId.Value);

			var trans = await query.OrderByDescending(trans => trans.Fecha).ToListAsync();

			var transaccionDto = trans.Select(trans => new TransaccionDTO
			{
				ClienteId = trans.ClienteId,
				Tipo = trans.Tipo,
				CodigoCripto = trans.CodigoCripto,
				Fecha = trans.Fecha,
				CantidadCripto = trans.CantidadCripto,
				Monto = trans.Monto
			});

			return Ok(trans);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<TransaccionDTO>> GetById(int id)
		{
			var trans = await _context.transacciones.Include(trans => trans.ClienteId).FirstOrDefaultAsync(trans => trans.Id == id);

			if (trans == null) return NotFound();

			var dto = new TransaccionDTO
			{
				ClienteId = trans.ClienteId,
				Tipo = trans.Tipo,
				CodigoCripto = trans.CodigoCripto,
				Fecha = trans.Fecha,
				CantidadCripto = trans.CantidadCripto,
				Monto = trans.Monto
			};

			return Ok(dto);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> EditarTransaccion(int id, [FromBody] TransaccionDTO dto)
		{
			var trans = await _context.transacciones.FindAsync(id);
			if (trans == null)
				return NotFound($"Transacción {id} no encontrada");

			await _context.SaveChangesAsync();

			var res = new TransaccionDTO
			{
				ClienteId = trans.ClienteId,
				Tipo = trans.Tipo,
				CodigoCripto = trans.CodigoCripto,
				Fecha = trans.Fecha,
				CantidadCripto = trans.CantidadCripto,
				Monto = trans.Monto
			};

			return Ok($"Transacción {id} modificada correctamente. '\n' {res}");
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> BorrarTransaccion(int id)
		{
			var trans = await _context.transacciones.FindAsync(id);

			if (trans == null)
				return NotFound($"Transacción {id} no encontrada.");

			_context.transacciones.Remove(trans);

			await _context.SaveChangesAsync();

			var res = new TransaccionDTO
			{
				ClienteId = trans.ClienteId,
				Tipo = trans.Tipo,
				CodigoCripto = trans.CodigoCripto,
				Fecha = trans.Fecha,
				CantidadCripto = trans.CantidadCripto,
				Monto = trans.Monto
			};

			return Ok($"Transacción {id} eliminada correctamente. '\n' {res}");
		}

	}
}