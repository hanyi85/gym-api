namespace gym_api.DTO
{
    public class SCartItemListDTO
    {
        public int CartId { get; set; }      
        public int SpecId { get; set; }      
        public string Name { get; set; }     
        public decimal Price { get; set; }   
        public decimal OriginPrice { get; set; } 
        public int Quantity { get; set; }    
        public string Image { get; set; }    
        public decimal Subtotal => Price * Quantity;
    }
}
