namespace RedStar.Amounts.StandardUnits
{
    [UnitDefinitionClass]
    public static class LuminousIntensityUnits
    {
        public static readonly Unit Candela = new Unit("candela", "cd", SIUnitTypes.LuminousIntensity);
        public static readonly Unit Lux = new Unit("lux", "lx", SIUnitTypes.LuminousIntensity);
    }
}