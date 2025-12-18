namespace CRIPTObackend.DTOs
{
	public class TransaccionDTO
	{
		public string CodigoCripto { get; set; }
		public int ClienteId { get; set; }
		public decimal CantidadCripto { get; set; }
		public DateTime Fecha { get; set; }
		public string Tipo { get; set; }
		public Decimal Monto { get; set; }
	}
}
