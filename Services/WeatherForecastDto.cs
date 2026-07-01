namespace ScubaHub.AdminWeb.Services;

// Mirrors the JSON shape returned by ScubaHub.Api's WeatherForecast record.
// TemperatureF is computed on the backend and sent over the wire, so it is a
// plain settable property here (we only ever deserialize it).
public class WeatherForecastDto
{
    public string Date { get; set; } = "";
    public int TemperatureC { get; set; }
    public int TemperatureF { get; set; }
    public string? Summary { get; set; }
}
