namespace MyRapidApiProject.Models
{
    public class HotelViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public string ReviewScoreWord { get; set; }
        public decimal ReviewScore { get; set; }
        public int ReviewCount { get; set; }
        public List<HotelPhotoViewModel> Photos { get; set; }
    }
}
