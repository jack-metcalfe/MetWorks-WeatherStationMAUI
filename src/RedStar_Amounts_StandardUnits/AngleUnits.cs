namespace RedStar.Amounts.StandardUnits
{
    [UnitDefinitionClass]
    public static class AngleUnits
    {
        public static readonly Unit Degree = new Unit("degree", "°", NonSIUnitTypes.Angle);
        public static readonly Unit Radian = new Unit("radian", "rad", NonSIUnitTypes.Angle);
    }
}
