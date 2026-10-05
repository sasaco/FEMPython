using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    internal static class DisplacementTableLayout
    {
        internal static void FitNodeIds(PdfDocument document, Table table, IEnumerable<string> ids,
            int componentCount, bool paired = false, bool hasCombination = false)
        {
            double width = ids.Where(id => !string.IsNullOrEmpty(id))
                .Select(id => document.MeasureString(id).Width + 6).DefaultIfEmpty(0).Max();
            if (width <= table.ColWidth[0]) return;

            // Canonical generated-node IDs are longer than the legacy numeric IDs.
            // Use one panel for 2D results instead of squeezing two unreadable ID columns.
            if (paired) table.ReDim(col: componentCount + 1);
            table.ColWidth[0] = width;
            double overflow = Math.Max(0, table.GetTableWidth() - document.currentPageSize.Width);
            if (hasCombination && overflow > 0)
            {
                int last = table.Columns - 1;
                double reclaimed = Math.Min(overflow, Math.Max(0, table.ColWidth[last] - 80));
                table.ColWidth[last] -= reclaimed;
                overflow -= reclaimed;
            }
            if (overflow > 0)
            {
                double reduction = overflow / componentCount;
                for (int column = 1; column <= componentCount; column++)
                {
                    if (table.ColWidth[column] - reduction < 55)
                        throw new InvalidOperationException("A displacement node ID is too wide for the selected paper.");
                    table.ColWidth[column] -= reduction;
                }
            }
        }
    }
}
