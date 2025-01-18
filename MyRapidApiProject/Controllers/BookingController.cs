using Microsoft.AspNetCore.Mvc;
using MyRapidApiProject.Models;
using Newtonsoft.Json;

namespace MyRapidApiProject.Controllers
{
    public class BookingController : Controller
    {
        private const string ApiKey = "d0cfbbf392msh72b21e1edf18b0bp16416bjsn6ff67cd1187d";
        private const string ApiHost = "booking-com18.p.rapidapi.com";
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(string query, string checkinDate, string checkoutDate, int adults, int children)
        {
            var locationId = await GetLocationId(query);
            if (locationId == null)
            {
                return View("Error", "Şehir adı bulunamadı.");
            }

            var hotels = await GetHotels(locationId, checkinDate, checkoutDate, adults, children);
            return View("Results", hotels);
        }

        private async Task<string> GetLocationId(string query)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{ApiHost}/stays/auto-complete?query={query}"),
                Headers =
            {
                { "x-rapidapi-key", ApiKey },
                { "x-rapidapi-host", ApiHost },
            },
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(body);

            return data?.data[0]?.id;
        }

        private async Task<List<HotelViewModel>> GetHotels(string locationId, string checkinDate, string checkoutDate, int adults, int children)
        {
            if (!DateTime.TryParse(checkinDate, out DateTime checkinDateParsed) ||
                !DateTime.TryParse(checkoutDate, out DateTime checkoutDateParsed))
            {
                throw new ArgumentException("Check-in veya check-out tarihi geçerli bir formatta değil.");
            }

            string formattedCheckinDate = checkinDateParsed.ToString("yyyy-MM-dd");
            string formattedCheckoutDate = checkoutDateParsed.ToString("yyyy-MM-dd");

            using var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{ApiHost}/stays/search?locationId={locationId}&checkinDate={formattedCheckinDate}&checkoutDate={formattedCheckoutDate}&adults={adults}&children={children}&units=metric&temperature=c"),
                Headers =
        {
            { "x-rapidapi-key", ApiKey },
            { "x-rapidapi-host", ApiHost },
        },
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(body);

            var hotels = new List<HotelViewModel>();
            foreach (var item in data?.data)
            {
                var hotel = new HotelViewModel
                {
                    Id = item.id,
                    Name = item.name,
                    PhotoUrl = item.photoUrls[0],
                    ReviewScoreWord = item.reviewScoreWord,
                    ReviewScore = item.reviewScore,
                    ReviewCount = item.reviewCount
                };

                var photos = await GetHotelPhotos(hotel.Id);
                if (photos.Count > 0)
                {
                    hotel.PhotoUrl = photos[0].PhotoUrl;
                }

                hotels.Add(hotel);
            }

            return hotels;
        }

        private async Task<List<HotelPhotoViewModel>> GetHotelPhotos(string hotelId)
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{ApiHost}/stays/get-photos?hotelId={hotelId}"),
                Headers =
        {
            { "x-rapidapi-key", ApiKey },
            { "x-rapidapi-host", ApiHost },
        },
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<dynamic>(body);

            var photos = new List<HotelPhotoViewModel>();

            try
            {
                var photoData = data?.data?.data?[hotelId];
                if (photoData != null && photoData.Count > 0)
                {
                    var firstItem = photoData[0];
                    var fourthIndex = firstItem[4];
                    var photoUrl = fourthIndex[31]?.ToString();

                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        var fullPhotoUrl = $"{data?.data?.url_prefix}{photoUrl}";

                        photos.Add(new HotelPhotoViewModel
                        {
                            PhotoUrl = fullPhotoUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fotoğraf alma işlemi sırasında hata oluştu: {ex.Message}");
            }

            return photos;
        }
    }

}