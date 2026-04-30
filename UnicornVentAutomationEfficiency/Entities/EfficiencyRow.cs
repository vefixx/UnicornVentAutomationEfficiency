namespace UnicornVentAutomationEfficiency.Entities;

public class EfficiencyRow
{
    public string VentOnTs { get; set; }
    public string VentOffTs { get; set; }
    public int BuildingId { get; set; }
    public string Building { get; set; }
    public string Complex { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentNo { get; set; }
    public double Co2PpmMin { get; set; }
    public double EfficiencyPct { get; set; }
    public int DurationHours { get; set; }
}