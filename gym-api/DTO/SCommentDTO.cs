namespace gym_api.DTO
{
    public class SCommentDTO
    {
        public int ComId { get; set; }           
        public int UserId { get; set; }          
        public int CommentStar { get; set; }     
        public string ProductComment { get; set; } 
        public string CommentTime { get; set; }    

        // 如果未來有建立 User 表關聯，可以增加此欄位顯示名稱
        public string UserName { get; set; }
    }
}
