using System;
using System.Collections.Generic;
using System.Linq;

namespace TwitchTools.Utils
{
    public class Table
    {
        public List<TableHeader> Headers { get; }
        public List<TableRow> Rows { get; }

        public Table(List<TableHeader> headers, List<TableRow> rows = null)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Rows = rows ?? new List<TableRow>();
        }
    }

    public class TableHeader
    {
        public string Name { get; }
        public int Padding { get; }

        public TableHeader(string name, int padding)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Padding = padding;
        }
    }

    public class TableRow
    {
        public List<string> Data { get; }

        public TableRow(List<string> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public TableRow(IEnumerable<object> data)
        {
            Data = data?.Select(x => x.ToString()).ToList() ?? throw new ArgumentNullException(nameof(data));
        }
    }
}
