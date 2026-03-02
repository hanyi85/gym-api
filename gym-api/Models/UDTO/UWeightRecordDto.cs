namespace gym_api.Models.UDTO
{
    public class UWeightRecordDto
    {
        public DateOnly RecordDate { get; set; }
        public decimal Weight { get; set; }
        public decimal? BodyFat { get; set; }
        public decimal? MuscleMass { get; set; }
    }
}
