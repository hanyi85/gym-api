namespace gym_api.DTO
{
    public class SOrderDTO
    {
        public string mName {  get; set; }
        public string mPhone { get; set; }
        public string email { get; set; }
        public string mAddress { get; set; }
        public int shipFee { get; set; }
        public int total { get; set; }
        public string note { get; set; }
        public List<SOrderDetailDTO> items { get; set; }
        public int payId {  get; set; }
        public int shipId {  get; set; }
    }
    public class SOrderDetailDTO
    {
        public int specId { get; set; }
        public string pName { get; set; }
        public int price { get; set; }
        public int quantity { get; set; }
        public int odId { get; set; }
    }
}
