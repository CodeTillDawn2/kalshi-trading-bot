using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{

    public class ExchangeScheduleResponse
    {
        public ExchangeSchedule Schedule { get; set; } = new();
    }
}
