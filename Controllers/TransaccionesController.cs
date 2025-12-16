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
				decimal totalCompras = await _context.transacciones
					.Where(t => t.ClienteId == transaccionDto.ClienteId && t.CodigoCripto == transaccionDto.CodigoCripto && t.Tipo == "compra")
					.SumAsync(t => t.CantidadCripto);

				decimal totalVentas = await _context.transacciones
					.Where(t => t.ClienteId == transaccionDto.ClienteId && t.CodigoCripto == transaccionDto.CodigoCripto && t.Tipo == "venta")
					.SumAsync(t => t.CantidadCripto);

				if (transaccionDto.CantidadCripto > (totalCompras - totalVentas))
					return BadRequest("No se puede vender más de lo que posee el cliente.");
			}

			var http = new HttpClient();
			string url = $"https://criptoya.com/api/{transaccionDto.CodigoCripto}/ars/1";
			var response = await http.GetStringAsync(url);
			var json = JObject.Parse(response);

			Console.WriteLine(json);

			decimal mejorPrecio = 0;

			foreach (var exchange in json)
			{
				var datos = exchange.Value;

				// Ignorar nodos que no contienen precios
				if (datos["totalAsk"] == null && datos["totalBid"] == null)
					continue;

				string askStr = datos["totalAsk"]?.ToString() ?? "0";
				string bidStr = datos["totalBid"]?.ToString() ?? "0";

				decimal ask = 0, bid = 0;

				// Convertir Infinity, NaN o vacíos a 0
				decimal.TryParse(askStr, NumberStyles.Any, CultureInfo.InvariantCulture, out ask);
				decimal.TryParse(bidStr, NumberStyles.Any, CultureInfo.InvariantCulture, out bid);

				mejorPrecio = Math.Max(mejorPrecio, Math.Max(ask, bid));
			}


			if (mejorPrecio == 0)
				return BadRequest("No hay precio disponible para esta criptomoneda en este momento. ");

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
				query = query.Where(t => t.ClienteId == clienteId.Value);

			var lista = await query
				.OrderByDescending(t => t.Fecha)
				.ToListAsync();

			var http = new HttpClient();

			var resultado = new List<object>();

			foreach (var t in lista)
			{
				// Precio actual desde CriptoYa
				string urlPrecioCrypto = $"https://criptoya.com/api/{t.CodigoCripto}/ars/1";
				var response = await http.GetStringAsync(urlPrecioCrypto);
				var json = JObject.Parse(response);

				decimal mejorPrecio = json.Properties()
					.Select(p => (decimal?)p.Value["totalAsk"])
					.Where(v => v.HasValue)
					.Max() ?? 0;

				decimal montoActual = mejorPrecio * t.CantidadCripto;

				resultado.Add(new
				{
					id = t.Id,
					tipo = t.Tipo,
					codigoCripto = t.CodigoCripto,
					cantidadCripto = t.CantidadCripto,
					fecha = t.Fecha.ToString("yyyy-MM-ddTHH:mm:ss"),
					clienteId = t.ClienteId,

					// monto guardado en la DB (lo que costó al momento de la compra)
					montoOriginal = t.Monto,

					// monto actualizado según CriptoYa
					montoActualizado = montoActual
				});
			}

			return Ok(resultado);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> ObtenerTransaccion(int id)
		{
			var transaccion = await _context.transacciones.FindAsync(id);
			if (transaccion == null)
				return NotFound("Transacción no encontrada.");

			return Ok(new
			{
				id = transaccion.Id,
				tipo = transaccion.Tipo,
				codigoCripto = transaccion.CodigoCripto,
				clienteId = transaccion.ClienteId,
				cantidadCripto = transaccion.CantidadCripto,
				monto = transaccion.Monto,
				fecha = transaccion.Fecha
			});
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> EditarTransaccion(int id, [FromBody] TransaccionDTO dto)
		{
			var trans = await _context.transacciones.FindAsync(id);
			if (trans == null)
				return NotFound("Transacción no encontrada");

			trans.CodigoCripto = dto.CodigoCripto;
			trans.CantidadCripto = dto.CantidadCripto;
			trans.Tipo = dto.Tipo;
			trans.ClienteId = dto.ClienteId;  

			await _context.SaveChangesAsync();
			return Ok("Transacción modificada");
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> BorrarTransaccion(int id)
		{
			var transaccion = await _context.transacciones.FindAsync(id);
			if (transaccion == null)
				return NotFound("Transacción no encontrada.");

			_context.transacciones.Remove(transaccion);
			await _context.SaveChangesAsync();

			return Ok("Transacción eliminada correctamente.");
		}

	}
}