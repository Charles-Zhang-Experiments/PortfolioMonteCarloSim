/* This data type is authored by Charles Zhang © Copyright 2022, 
 * borrowed from the Parcel (A Workflow Engine) project. 
 * It's used here with special permission from the author.
 * Compared with native .Net DataGrid objects or other available libraries,
 * DataGrid is cleaner and serves the purpose of containing generic data well.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using Csv;

namespace Parcel.Shared.DataTypes
{
    public class DataColumn
    {
        #region Construction
        public DataColumn(){}
        public DataColumn(string header) => Header = header.Trim().Trim('"');
        public DataColumn(DataColumn other)
        {
            Header = other.Header;
            _columnData = other._columnData.ToList();
            _columnType = other._columnType;
        }
        public DataColumn MakeCopy()
            => new DataColumn(this);
        public void RenameHeader(string newName)
            => Header = newName;
        #endregion

        #region Properties
        public string Header { get; private set; }
        private List<dynamic> _columnData { get; } = new List<dynamic>();
        private Type _columnType { get; set; }
        #endregion

        #region Accessor
        public void Add<T>(T value)
        {
            if (_columnData.Count == 0)
                _columnType = value.GetType();

            if (value.GetType() != _columnType)
                _columnType = null; // throw new ArgumentException("Wrong type.");
            _columnData.Add(value);
        }
        public int Length => _columnData.Count;
        public dynamic this[int index] => _columnData[index];
        public IEnumerable<T> GetDataAs<T>() => _columnData.OfType<T>();
        public string TypeName
        {
            get
            {
                if (_columnType == null) return "Mixed";
                else if (_columnType == typeof(double)) return "Number";
                return _columnType.Name;
            }
        }
        public Type Type => _columnType;
        #endregion

        #region Column Operations (Math)
        public double Mean()
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            IEnumerable<double> list = _columnData.Cast<double>();
            return list.Average();
        }
        public double Variance(bool population)
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            double[] values = _columnData.Cast<double>().ToArray();
            double variance = 0.0;
            if (values.Count() > 1)
            {
                double avg = values.Average();
                variance += values.Sum(value => Math.Pow(value - avg, 2.0));
            }
            return variance / (population ? values.Length - 1 : values.Length); // For population, use n-1, for sample, use n
        }
        public double STD(bool population)
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            IEnumerable<double> list = _columnData.Cast<double>();
            return Math.Sqrt(Variance(population));
        }
        public double Min()
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            IEnumerable<double> list = _columnData.Cast<double>();
            return list.Min();
        }
        public double Max()
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            IEnumerable<double> list = _columnData.Cast<double>();
            return list.Max();
        }
        public double Sum()
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");

            IEnumerable<double> list = _columnData.Cast<double>();
            return list.Sum();
        }
        public double Correlation(DataColumn other)
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");
            if (this.Length != other.Length)
                throw new InvalidOperationException("Columns are not of same length.");

            double covariance = this.Covariance(other);
            double std1 = this.STD(true);   // Always use n-1 for population
            double std2 = other.STD(true);
            return covariance / (std1 * std2);
        }
        public double Covariance(DataColumn other)
        {
            if (_columnType != typeof(double))
                throw new InvalidOperationException("Column is not of numerical type.");
            if (this.Length != other.Length)
                throw new InvalidOperationException("Columns are not of same length.");

            double[] values1 = _columnData.Cast<double>().ToArray();
            double[] values2 = other._columnData.Cast<double>().ToArray();
            double variance = 0.0;
            if (values1.Count() > 1)
            {
                double avg1 = values1.Average();
                double avg2 = values1.Average();
                for (int i = 0; i < values1.Length; i++)
                    variance += (values1[i] - avg1) * (values2[i] - avg2);
            }
            return variance / (values1.Length - 1); // Always use n-1 for population
        }
        #endregion
    }
    
    public class DataGrid
    {
        #region Helper
        public static object Preformatting(string inputValue)
        {
            // Perform pre-formatting
            if (double.TryParse(inputValue, out double number))
                return number;
            else if (DateTime.TryParse(inputValue, out DateTime dateTime))
                return dateTime;
            else return inputValue;
        }
        #endregion
        
        #region Constructors
        public DataGrid(){}
        public DataGrid(ExpandoObject expando)
        {
            var dict = (IDictionary<string, object>)expando;
            foreach (string key in dict.Keys)
            {
                var col = new DataColumn(key);
                col.Add(dict[key]);
                Columns.Add(col);
            }
        }
        public DataGrid(IEnumerable<ICsvLine> csvLines)
        {
            string[] headers = null;
            foreach (ICsvLine line in csvLines)
            {
                // Initialize columns
                if (headers == null)
                {
                    headers = line.Headers;
                    foreach (string header in headers)
                        Columns.Add(new DataColumn(header));
                }
                
                // Add data to columns
                for (var i = 0; i < headers.Length; i++)
                {
                    Columns[i].Add(Preformatting(line[i]));
                }
            }
        }
        public DataGrid(DataSet dataset, bool forceFirstLineAsHeader = false)
        {
            DataTable table = dataset.Tables[0];
            // TableName = table.TableName;

            // Initialize columns
            List<string> headers = new List<string>();
            if (!forceFirstLineAsHeader)
            {
                foreach (System.Data.DataColumn column in table.Columns)
                {
                    headers.Add(column.Caption);
                    Columns.Add(new DataColumn(column.Caption));
                }    
            }
            else
            {
                DataRow row = table.Rows[0];
                for (int index = 0; index < table.Columns.Count; index++)
                {
                    string text = row[index].ToString();
                    headers.Add(text);
                    Columns.Add(new DataColumn(text));
                }
            }

            // Populate row data
            int startingIndex = forceFirstLineAsHeader ?  1 : 0;
            for (int i = startingIndex; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                // Add data to columns
                for (var col = 0; col < headers.Count; col++)
                    Columns[col].Add(row[col]);   
            }
        }
        #endregion

        #region Members
        // public string TableName { get; set; }
        public List<DataColumn> Columns { get; set; } = new List<DataColumn>();
        public DataColumn OptionalRowHeaderColumn { get; set; }
        #endregion

        #region Accessors
        public int ColumnCount => Columns.Count;
        public int RowCount => Columns.First().Length;
        public List<dynamic> Rows
        {
            get
            {
                string[] columnHeaders = Columns.Select(c => c.Header).ToArray();
                List<dynamic> rows = new List<dynamic>();
                for (int row = 0; row < RowCount; row++)
                {
                    Dictionary<string, int> repeatNameCounter = new Dictionary<string, int>();
                    dynamic temp = new ExpandoObject();
                    if (OptionalRowHeaderColumn != null)
                        ((IDictionary<String, Object>)temp)[PreProcessColumnNameForDisplay(OptionalRowHeaderColumn.Header, repeatNameCounter)] = OptionalRowHeaderColumn[row];
                    for (int col = 0; col < ColumnCount; col++)
                        ((IDictionary<String, Object>)temp)[PreProcessColumnNameForDisplay(columnHeaders[col], repeatNameCounter)] = Columns[col][row];
                    rows.Add(temp);
                }
                return rows;
            }
        }

        public string ToCSV()
        {
            Dictionary<string, int> repeatNameCounter = new Dictionary<string, int>();
            StringBuilder builder = new StringBuilder();
            if (OptionalRowHeaderColumn != null) builder.Append($"{PreProcessColumnNameForDisplay(OptionalRowHeaderColumn.Header, repeatNameCounter)},");
            builder.AppendLine(string.Join(',', Columns.Select(c => PreProcessColumnNameForDisplay(c.Header, repeatNameCounter))));
            
            for (int row = 0; row < RowCount; row++)
            {
                if (OptionalRowHeaderColumn != null)
                    builder.Append(OptionalRowHeaderColumn[row].ToString() + ',');
                for (int col = 0; col < ColumnCount; col++)
                    builder.Append(Columns[col][row].ToString() + ',');
                
                builder.Remove(builder.Length - 1, 1);
                builder.AppendLine();
            }

            return builder.ToString();
        }
        #endregion

        #region Editors (In-Place Operations)
        public void AddRow(params object[] values)
        {
            if (values.Length > Columns.Count)
                throw new ArgumentException("Wrong number of row elements.");

            for (int i = 0; i < values.Length; i++)
            {
                Columns[i].Add(values[i]);
            }
        }
        public DataColumn AddColumn(string columnName)
        {
            var newColumn = new DataColumn(columnName);
            Columns.Add(newColumn);
            return newColumn;
        }
        public DataColumn RemoveColumn(string extra)
        {
            if (Columns.Any(c => c.Header == extra))
                Columns.Remove(Columns.First(c => c.Header == extra));
            return null;
        }
        public void AddOptionalRowHeaderColumn(string columnName)
        {
            OptionalRowHeaderColumn = new DataColumn(columnName);
        }
        public DataColumn AddColumnFrom(DataColumn refColumn, int rowCount)
        {
            var column = new DataColumn(refColumn.Header);
            var count = rowCount == 0 ? refColumn.Length : rowCount;
            for (int i = 0; i < count; i++)
                column.Add(refColumn[i]);
            Columns.Add(column);
            return column;
        }
        public void Sort(string anchorColumnName, bool reverseOrder)
        {
            var result = reverseOrder 
                ? Rows.OrderByDescending(r => ((IDictionary<String, Object>) r)[anchorColumnName]).ToArray()
                : Rows.OrderBy(r => ((IDictionary<String, Object>) r)[anchorColumnName]).ToArray();
            var names = Columns.Select(c => c.Header);
            Columns = names.Select(name =>
            {
                var col = new DataColumn(name);
                foreach (dynamic expando in result)
                    col.Add(((IDictionary<String, Object>) expando)[name]);
                return col;
            }).ToList();
        }
        #endregion

        #region Copy Operations
        public DataGrid MakeCopy()
        {
            DataGrid result = new DataGrid();
            IEnumerable<DataColumn> columnCopies = this.Columns
                .Select(c => c.MakeCopy());
            result.Columns = columnCopies.ToList();
            return result;
        }
        public DataGrid Append(DataGrid other)
        {
            DataGrid result = MakeCopy();
            result.Columns.AddRange(other.Columns.Select(c => c.MakeCopy()));
            return result;
        }
        public DataGrid Extract(IEnumerable<int> columnIndex)
        {
            DataGrid result = new DataGrid();
            IEnumerable<DataColumn> columnCopies = columnIndex.Select(i => this.Columns[i].MakeCopy());
            result.Columns = columnCopies.ToList();
            return result;
        }
        public DataGrid Extract(string[] names)
        {
            DataGrid result = new DataGrid();
            IEnumerable<DataColumn> columnCopies = this.Columns
                .Where(c => names.Contains(c.Header))
                .Select(c => c.MakeCopy());
            result.Columns = columnCopies.ToList();
            return result;
        }
        public DataGrid Exclude(IEnumerable<int> columnIndex)
            => this.Extract(Columns.Select((c, i) => i).Except(columnIndex));
        public DataGrid Exclude(string[] names)
            => this.Extract(Columns.Select(c => c.Header).Except(names).ToArray());
        public DataGrid Transpose()
        {
            DataGrid result = new DataGrid();
            // Create optional column to hold existing headers
            result.AddOptionalRowHeaderColumn("Header");
            foreach (DataColumn column in this.Columns)
                result.OptionalRowHeaderColumn.Add(column.Header);
            
            // Create data columns
            for (int i = 0; i < this.Columns.First().Length; i++)
                result.AddColumn($"Value {i+1}");
                
            // Copy values over
            foreach (DataColumn column in this.Columns)
                for (int row = 0; row < column.Length; row++)
                    result.Columns[row].Add(column[row]);

            return result;
        }
        public DataGrid MatrixMultiply(DataGrid other)
        {
            // Make use of only numerical columns
            var numericalColumns = this.Columns.Where(c => c.Type == typeof(double)).ToArray();
            var numericalColumnsOther = other.Columns.Where(c => c.Type == typeof(double)).ToArray();
            int firstMatrixColumnCount = numericalColumns.Length; 
            int secondMatrixRowCount = other.RowCount;

            if (firstMatrixColumnCount != secondMatrixRowCount)
                throw new InvalidOperationException("Matrix dimensions don't match for multiplication operation.");

            // Initialize columns
            DataGrid result = new DataGrid();
            for (var i = 0; i < numericalColumnsOther.Length; i++)
                result.AddColumn($"Column {i + 1}");
            
            // Compute rows
            for (int row = 0; row < this.RowCount; row++)
            {
                List<double> rowElements = new List<double>();
                for (int col = 0; col < numericalColumnsOther.Length; col++)
                {
                    double sum = 0;
                    for (int i = 0; i < firstMatrixColumnCount; i++)
                        sum += this.Columns[i][row] * other.Columns[col][i];
                    rowElements.Add(sum);
                }
                result.AddRow(rowElements.OfType<object>().ToArray());
            }

            return result;
        }
        #endregion

        #region Numerical Computation
        public DataGrid CovarianceMatrix()
        {
            DataGrid result = new DataGrid();
            DataColumn[] numericalColumns = Columns.Where(c => c.Type == typeof(double)).ToArray();
            
            // Define columns
            result.AddOptionalRowHeaderColumn("Relation");
            foreach (DataColumn column in numericalColumns)
                result.AddColumn(column.Header);
            // Compute data
            foreach (DataColumn column in numericalColumns)
            {
                result.OptionalRowHeaderColumn.Add(column.Header);
                result.AddRow(numericalColumns.Select(other => other.Covariance(column)).OfType<object>().ToArray());
            }
            
            return result;
        }
        #endregion

        #region Routines
        public struct ColumnInfo
        {
            public string NewKey { get; set; }
            public string OriginalHeader { get; set; }
            public int ColumnIndex { get; set; }
            public string TypeName { get; set; }
        }
        public Dictionary<string, ColumnInfo> GetColumnInfoForDisplay()
        {
            Dictionary<string, int> nameCounter = new Dictionary<string, int>();
            IEnumerable<Tuple<string, string, int, string>> infoTuple = Columns.Select((c, i)=> 
                new Tuple<string, string, int, string>(
                    PreProcessColumnNameForDisplay(c.Header, nameCounter),
                    c.Header, 
                    i,
                    c.TypeName));
            Dictionary<string, ColumnInfo> dict = infoTuple.ToDictionary(
                t => t.Item1, 
                t => new ColumnInfo()
                {
                    NewKey = t.Item1,
                    OriginalHeader = t.Item2,
                    ColumnIndex = t.Item3,
                    TypeName = t.Item4
                });
            if (OptionalRowHeaderColumn != null)
                dict[OptionalRowHeaderColumn.Header] = new ColumnInfo()
                {
                    NewKey = OptionalRowHeaderColumn.Header, 
                    OriginalHeader = OptionalRowHeaderColumn.Header, 
                    ColumnIndex = -1,
                    TypeName = OptionalRowHeaderColumn.TypeName
                };
            return dict;
        }
        private string PreProcessColumnNameForDisplay(string original, Dictionary<string, int> nameCounter)
        {
            if (!nameCounter.ContainsKey(original))
                nameCounter[original] = 1;
            else 
                nameCounter[original] = nameCounter[original] + 1;

            return
                $"{original}{(nameCounter[original] == 1 ? string.Empty : $"{nameCounter[original]}")}";
        }
        #endregion
    }
}