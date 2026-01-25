namespace RedStar.Amounts.StandardUnits
{
   [UnitDefinitionClass]
    public static class AreaUnits
    {
        // canonical base unit
        public static readonly Unit SquareMeter = new Unit("square meter", "m²", NonSIUnitTypes.Area);

        // derived units
        public static readonly Unit SquareFoot = new Unit("square foot", "ft²", 0.092903 * SquareMeter);
        public static readonly Unit SquareInch = new Unit("square inch", "in²", 0.00064516 * SquareMeter);
        public static readonly Unit SquareYard = new Unit("square yard", "yd²", 0.836127 * SquareMeter);
        public static readonly Unit Acre = new Unit("acre", "ac", 4046.8564224 * SquareMeter);
        public static readonly Unit Hectare = new Unit("hectare", "ha", 10000 * SquareMeter);
        public static readonly Unit SquareKilometer = new Unit("square kilometer", "km²", 1_000_000 * SquareMeter);
        public static readonly Unit SquareMile = new Unit("square mile", "mi²", 2_589_988.110336 * SquareMeter);
    }
}