using System.ComponentModel.DataAnnotations.Schema;

namespace CRIPTObackend.Models
{
	public class Transaccion
	{
		public int Id { get; set; }
		public string CodigoCripto { get; set; }// bitcoin, usdc, ethereum
		public string Tipo { get; set; } // compra o venta
		public int ClienteId { get; set; }

		[Column(TypeName = "decimal(18,8)")]
		public decimal CantidadCripto { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Monto { get; set; } // Monto total pagado en pesos
		public DateTime Fecha { get; set; }
	}
}
