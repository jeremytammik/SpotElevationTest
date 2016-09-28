using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SpotElevationTest
{
    /// <summary>
    /// A wrapper of family symbol
    /// </summary>
    public class FamilySymbolWrapper
    {
        public string name { get; set; }
        public string familyName { get; set; }
        public string id { get; set; }
        public string categoryName { get; set; }
        public FamilySymbol familySymbol { get; set; }
        public List<Parameter> parameters { get; set; }
        public string attachmentPosition { get; set; }
    }
}
