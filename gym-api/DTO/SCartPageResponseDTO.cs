namespace gym_api.DTO
{
    public class SCartPageResponseDTO
    {
        public List<SCartItemListDTO> Items { get; set; } = new List<SCartItemListDTO>();
        public decimal Subtotal { get; set; }    
        public int ShippingFee { get; set; }     
        public decimal TotalAmount { get; set; } 
    }
}
