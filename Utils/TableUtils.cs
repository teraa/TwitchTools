using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchToolsV2
{
    public static class TableUtils
    {
        public static void Print(this Table table, TablePrintOptions options)
        {
            if (options.Borders.HasFlag(TableBorders.Top))
                PrintHorizontalDivider(table.Headers, options);

            if (options.PrintHeader)
            {
                PrintHeaders(table.Headers, options);
                if (options.Borders.HasFlag(TableBorders.Top) || options.Borders.HasFlag(TableBorders.Horizontal))
                    PrintHorizontalDivider(table.Headers, options);
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                PrintRow(table.Headers, table.Rows[i], options);

                if (options.Borders.HasFlag(TableBorders.Horizontal) && i != table.Rows.Count - 1)
                    PrintHorizontalDivider(table.Headers, options);
            }

            if (options.Borders.HasFlag(TableBorders.Bottom))
                PrintHorizontalDivider(table.Headers, options);
        }

        public static void PrintHeaders(IEnumerable<TableHeader> headers, TablePrintOptions options)
        {
            char divider = options.Borders.HasFlag(TableBorders.Vertical) ? options.ColDiv : ' ';

            var sb = new StringBuilder();
            foreach (var header in headers)
            {
                var value = header.Name;

                if (header.Padding < 0)
                    value = header.Name.PadRight(-header.Padding);
                else if (header.Padding > 0)
                    value = header.Name.PadLeft(header.Padding);

                sb.Append(value).Append(divider);
            }
            sb.Remove(sb.Length - 1, 1); // Remove trailing divider.

            if (options.Borders.HasFlag(TableBorders.Left))
                sb.Insert(0, options.ColDiv);

            if (options.Borders.HasFlag(TableBorders.Right))
                sb.Append(options.ColDiv);

            Console.WriteLine(sb);
        }
        public static void PrintHorizontalDivider(IEnumerable<TableHeader> headers, TablePrintOptions options)
        {
            var sb = new StringBuilder();

            if (options.Borders.HasFlag(TableBorders.Vertical))
            {
                foreach (var header in headers)
                    sb.Append(options.RowDiv, Math.Abs(header.Padding))
                        .Append(options.JunctDiv);

                sb.Remove(sb.Length - 1, 1); // Remove trailing divider.
            }
            else
            {
                sb.Append(options.RowDiv, headers.Select(x => Math.Abs(x.Padding)).Sum() + headers.Count() - 1);
            }

            if (options.Borders.HasFlag(TableBorders.Left))
                sb.Insert(0, options.JunctDiv);

            if (options.Borders.HasFlag(TableBorders.Right))
                sb.Append(options.JunctDiv);

            Console.WriteLine(sb);
        }
        public static void PrintRow(List<TableHeader> headers, TableRow row, TablePrintOptions options)
        {
            char divider;
            if (options.Borders.HasFlag(TableBorders.Vertical))
                divider = options.ColDiv;
            else
                divider = ' ';

            var sb = new StringBuilder();

            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                var value = row.Data[i];

                if (header.Padding < 0)
                    value = value.PadRight(-header.Padding);
                else if (header.Padding > 0)
                    value = value.PadLeft(header.Padding);

                sb.Append(value).Append(divider);
            }
            sb.Remove(sb.Length - 1, 1); // Remove trailing divider.

            if (options.Borders.HasFlag(TableBorders.Left))
                sb.Insert(0, options.ColDiv);

            if (options.Borders.HasFlag(TableBorders.Right))
                sb.Append(options.ColDiv);

            Console.WriteLine(sb);
        }
    }

    public class TablePrintOptions
    {
        public char RowDiv { get; set; } = '-';
        public char ColDiv { get; set; } = '|';
        public char JunctDiv { get; set; } = '+';
        public TableBorders Borders { get; set; } = TableBorders.Full;
        public bool PrintHeader { get; set; } = true;
    }

    [Flags]
    public enum TableBorders
    {
        None = 0,

        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Outline = Top | Bottom | Left | Right,

        Vertical = 16,
        Horizontal = 32,
        Internal = Vertical | Horizontal,

        Full = Outline | Internal
    }
}
