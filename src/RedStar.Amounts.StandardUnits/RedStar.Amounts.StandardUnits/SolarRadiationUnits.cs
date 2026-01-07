namespace RedStar.Amounts.StandardUnits
{
    [UnitDefinitionClass]
    public static class SolarRadiationUnits
    {
        public static readonly Unit WattPerSquareMeter = new (
            "watt/square meter",
            "W/m²",
            EnergyUnits.Watt / AreaUnits.SquareMeter);
        public static readonly Unit KilowattPerSquareMeter = new (
            "kilowatt/square meter",
            "kW/m²",
            EnergyUnits.KiloWatt / AreaUnits.SquareMeter);
    }
}