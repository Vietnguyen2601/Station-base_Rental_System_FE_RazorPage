// Temporary test file to check PayOS types
#if false
using PayOS;
using PayOS.Types;

namespace EVStationRental.Services.ExternalService.Services
{
    public class PayOSTest
    {
        public void Test()
        {
            var client = new PayOS("clientId", "apiKey", "checksumKey");
            var item = new ItemData("name", 1, 1000);
            var payment = new PaymentData(123, 1000, "desc", new List<ItemData> { item }, "cancel", "return");
        }
    }
}
#endif
