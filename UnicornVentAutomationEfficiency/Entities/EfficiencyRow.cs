using System.ComponentModel.DataAnnotations.Schema;

namespace UnicornVentAutomationEfficiency.Entities;

public class EfficiencyRow
{
    [Column("vent_on_ts")] public string VentOnTs { get; set; }
    [Column("vent_off_ts")] public string VentOffTs { get; set; }
    [Column("building_id")] public int BuildingId { get; set; }
    [Column("complex_id")] public int ComplexId { get; set; }
    [Column("apartment_id")] public int ApartmentId { get; set; }
    [Column("apartment_no")] public string ApartmentNo { get; set; }
    [Column("co2_ppm_min")] public double Co2PpmMin { get; set; }
    [Column("efficiency_pct")] public double EfficiencyPct { get; set; }
    [Column("duration_hours")] public int DurationHours { get; set; }
}