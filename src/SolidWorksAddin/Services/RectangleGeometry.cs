using System;
using System.Collections.Generic;
using System.Linq;

namespace SwCursor.SolidWorksAddin.Services
{
    // Coordinates are in sketch space, in millimetres. Native extraction owns COM.
    public sealed class SketchEdgeSnapshot
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double z1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
        public double z2 { get; set; }
    }

    public sealed class RectangleMeasurement
    {
        public bool valid { get; set; }
        public string message { get; set; }
        public double width_mm { get; set; }
        public double height_mm { get; set; }
    }

    public static class RectangleGeometry
    {
        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        private static RectangleMeasurement Invalid(string message) => new RectangleMeasurement { message = message };

        public static RectangleMeasurement Measure(IList<SketchEdgeSnapshot> edges)
        {
            if (edges == null || edges.Count != 4 || edges.Any(e => e == null))
                return Invalid("Expected exactly four non-construction straight edges.");
            if (edges.Any(e => !new[] { e.x1, e.y1, e.z1, e.x2, e.y2, e.z2 }.All(Finite)))
                return Invalid("Sketch coordinates are not finite.");
            double minX = edges.Min(e => Math.Min(e.x1, e.x2));
            double maxX = edges.Max(e => Math.Max(e.x1, e.x2));
            double minY = edges.Min(e => Math.Min(e.y1, e.y2));
            double maxY = edges.Max(e => Math.Max(e.y1, e.y2));
            double width = maxX - minX, height = maxY - minY;
            if (!CadValidation.Dimension(width) || !CadValidation.Dimension(height))
                return Invalid("Rectangle dimensions are outside the supported range.");
            double tolerance = Math.Min(1e-6, Math.Min(width, height) * 1e-6);
            var sides = new HashSet<int>();
            foreach (SketchEdgeSnapshot edge in edges)
            {
                if (Math.Abs(edge.z1) > tolerance || Math.Abs(edge.z2) > tolerance)
                    return Invalid("Expected a planar 2D sketch.");
                int a = Corner(edge.x1, edge.y1, minX, maxX, minY, maxY, tolerance);
                int b = Corner(edge.x2, edge.y2, minX, maxX, minY, maxY, tolerance);
                if (a < 0 || b < 0 || a == b)
                    return Invalid("Endpoints do not form four connected rectangle corners.");
                int low = Math.Min(a, b), high = Math.Max(a, b);
                int side = low * 4 + high;
                // Four boundary pairs: 0-1, 0-2, 1-3, 2-3. Diagonals are rejected.
                if (side != 1 && side != 2 && side != 7 && side != 11)
                    return Invalid("Diagonal or non-axis-aligned sketch edge.");
                if (!sides.Add(side)) return Invalid("Duplicate edge or a missing rectangle side.");
            }
            if (sides.Count != 4) return Invalid("Rectangle boundary is not closed.");
            return new RectangleMeasurement { valid = true, width_mm = width, height_mm = height,
                message = "Four unique sides form a closed rectangle." };
        }

        private static int Corner(double x, double y, double minX, double maxX, double minY, double maxY, double tolerance)
        {
            int ix = Math.Abs(x - minX) <= tolerance ? 0 : Math.Abs(x - maxX) <= tolerance ? 1 : -1;
            int iy = Math.Abs(y - minY) <= tolerance ? 0 : Math.Abs(y - maxY) <= tolerance ? 1 : -1;
            return ix < 0 || iy < 0 ? -1 : ix + 2 * iy;
        }
    }
}
