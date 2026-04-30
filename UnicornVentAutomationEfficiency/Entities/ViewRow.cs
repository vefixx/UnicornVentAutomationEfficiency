namespace UnicornVentAutomationEfficiency.Entities;

public class ViewRow
{
    public string Ts { get; set; }
    public int BuildingId { get; set; }
    public string Building { get; set; }
    public string Complex { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentNo { get; set; }
    public double Co2Ppm { get; set; }
    public bool VentOn { get; set; }
}