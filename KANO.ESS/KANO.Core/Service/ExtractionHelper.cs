using Aspose.Cells;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public enum DataType
    {
        Boolean,
        Int,
        DateTime,
        Double,
        String
    }

    public class ColumnProperties
    {
        public string OriginalTitle { get; set; }
        public string ChangeTitleTo { get; set; }

        public DataType ChangeDataTypeTTo { get; set; }
    }

    public class ExtractionHelper
    {
        public async Task<IList<BsonDocument>> ExtractMax(string Path, int SheetIndex = 0, int MaxColumn = 0, System.IO.FileMode FileMode = System.IO.FileMode.Open)
        {
            return await Task.Run(() =>
            {
                System.IO.FileStream fstream = File.Open(@Path, FileMode, FileAccess.Read, FileShare.ReadWrite);
                Workbook workbook = new Workbook(fstream);
                int ActivWorksheet = workbook.Worksheets.Count();
                Worksheet worksheet = workbook.Worksheets[SheetIndex];
                int maxcolumn = MaxColumn - 1;
                int maxRow = worksheet.Cells.MaxRow;

                string StartRange = worksheet.Cells.FirstCell.Name.ToString(); //CellsHelper.CellIndexToName(maxRow, maxcolumn);
                string EndRange = CellsHelper.CellIndexToName(maxRow, maxcolumn);

                Range range = worksheet.Cells.CreateRange(StartRange, EndRange);
                StringBuilder sb = new StringBuilder();

                for (int j = 1; j < range.RowCount; j++)
                {
                    sb.Append("{");
                    for (int h = 0; h < range.ColumnCount; h++)
                    {
                        string Json = "";
                        string value = string.Empty;
                        if (range[j, h].Type == CellValueType.IsNull ||
                            range[j, h].Type == CellValueType.IsString ||
                            range[j, h].Type == CellValueType.IsDateTime ||
                            range[j, h].Type == CellValueType.IsUnknown || range[j, h].Type == CellValueType.IsError)
                        {
                            if (range[j, h].Value != null)
                            {
                                value = range[j, h].Value.ToString().Replace("\"", "");
                                if (range[j, h].Type == CellValueType.IsDateTime)
                                {
                                    DateTime x = Tools.ToUTC(Convert.ToDateTime(range[j, h].Value.ToString()));
                                    value = x.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                            }
                            value = "\"" + value + "\"";

                            if (range[0, h].Type == CellValueType.IsDateTime)
                            {
                                DateTime x = Tools.ToUTC(Convert.ToDateTime(range[0, h].Value.ToString()));
                                Json = x.ToString("_yyyyMMddHHmmss");
                                if (h == range.ColumnCount - 1)
                                    Json = "\"" + Json + "\"" + ":" + value;
                                else
                                    Json = "\"" + Json + "\"" + ":" + value + ",";
                            }
                            else
                            {
                                if (h == range.ColumnCount - 1)
                                {
                                    Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\");
                                }
                                else
                                    Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\") + ",";
                            }

                            Json = Json.Replace("&", "");
                        }
                        else // Numeric Type
                        {
                            if (range[j, h].Value != null)
                            {
                                value = range[j, h].Value.ToString().Replace("\"", "").Replace(',', '.');
                            }
                            else
                            {
                                value = "0";
                            }
                            if (range[0, h].Type == CellValueType.IsDateTime)
                            {
                                DateTime x = Tools.ToUTC(Convert.ToDateTime(range[0, h].Value.ToString()));
                                Json = x.ToString("_yyyyMMddHHmmss");
                                if (h == range.ColumnCount - 1)
                                    Json = "\"" + Json + "\"" + ":" + value;
                                else
                                    Json = "\"" + Json + "\"" + ":" + value + ",";
                            }
                            else
                            {
                                if (h == range.ColumnCount - 1)
                                    Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\");
                                else
                                    Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\") + ",";
                            }
                            Json = Json.Replace("&", "");
                        }
                        sb.Append(Json);
                    }
                    if (j == range.RowCount - 1)
                        sb.Append("}");
                    else
                        sb.Append("},");
                }
                fstream.Close();

                string jsonStr = sb.ToString();
                List<string> spl = jsonStr.Split(new string[] { "}," }, StringSplitOptions.None).ToList<string>();
                List<BsonDocument> bson = new List<BsonDocument>();
                BsonDocument doc = new BsonDocument();
                foreach (string y in spl)
                {
                    string temp = string.Empty;
                    if (y.EndsWith("}") == false)
                        temp = y + @"}";
                    else
                        temp = y;

                    BsonDocument document = BsonDocument.Parse(temp);
                    bson.Add(document);
                }
                return bson;
            });
        }

        public IList<BsonDocument> ExtractMaxNotSync(string Path, int SheetIndex = 0, int MaxColumn = 0, System.IO.FileMode FileMode = System.IO.FileMode.Open)
        {

            System.IO.FileStream fstream = File.Open(@Path, FileMode, FileAccess.Read, FileShare.ReadWrite);
            Workbook workbook = new Workbook(fstream);
            int ActivWorksheet = workbook.Worksheets.Count();
            Worksheet worksheet = workbook.Worksheets[SheetIndex];
            int maxcolumn = MaxColumn - 1;
            int maxRow = worksheet.Cells.MaxRow;

            string StartRange = worksheet.Cells.FirstCell.Name.ToString(); //CellsHelper.CellIndexToName(maxRow, maxcolumn);
            string EndRange = CellsHelper.CellIndexToName(maxRow, maxcolumn);

            Range range = worksheet.Cells.CreateRange(StartRange, EndRange);
            StringBuilder sb = new StringBuilder();

            for (int j = 1; j < range.RowCount; j++)
            {
                sb.Append("{");
                for (int h = 0; h < range.ColumnCount; h++)
                {
                    string Json = "";
                    string value = string.Empty;
                    if (range[j, h].Type == CellValueType.IsNull ||
                        range[j, h].Type == CellValueType.IsString ||
                        range[j, h].Type == CellValueType.IsDateTime ||
                        range[j, h].Type == CellValueType.IsUnknown || range[j, h].Type == CellValueType.IsError ||
                        range[j, h].Type == CellValueType.IsBool)
                    {
                        if (range[j, h].Value != null)
                        {
                            value = range[j, h].Value.ToString().Replace("\"", "");
                            if (range[j, h].Type == CellValueType.IsDateTime)
                            {
                                DateTime x = Tools.ToUTC(Convert.ToDateTime(range[j, h].Value.ToString()));
                                value = x.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                        }
                        value = "\"" + value + "\"";

                        if (range[0, h].Type == CellValueType.IsDateTime)
                        {
                            DateTime x = Tools.ToUTC(Convert.ToDateTime(range[0, h].Value.ToString()));
                            Json = x.ToString("_yyyyMMddHHmmss");
                            if (h == range.ColumnCount - 1)
                                Json = "\"" + Json + "\"" + ":" + value;
                            else
                                Json = "\"" + Json + "\"" + ":" + value + ",";
                        }
                        else
                        {
                            if (h == range.ColumnCount - 1)
                            {
                                Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\");
                            }
                            else
                                Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\") + ",";
                        }

                        Json = Json.Replace("&", "");
                    }

                    else // Numeric Type
                    {
                        if (range[j, h].Value != null)
                        {
                            value = range[j, h].Value.ToString().Replace("\"", "").Replace(',', '.');
                        }
                        else
                        {
                            value = "0";
                        }
                        if (range[0, h].Type == CellValueType.IsDateTime)
                        {
                            DateTime x = Tools.ToUTC(Convert.ToDateTime(range[0, h].Value.ToString()));
                            Json = x.ToString("_yyyyMMddHHmmss");
                            if (h == range.ColumnCount - 1)
                                Json = "\"" + Json + "\"" + ":" + value;
                            else
                                Json = "\"" + Json + "\"" + ":" + value + ",";
                        }
                        else
                        {
                            if (h == range.ColumnCount - 1)
                                Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\");
                            else
                                Json = "\"" + range[0, h].Value.ToString().Replace(" ", "_").Replace(".", " ") + "\"" + ":" + value.Replace("\\", "\\\\") + ",";
                        }
                        Json = Json.Replace("&", "");
                    }
                    sb.Append(Json);
                }
                if (j == range.RowCount - 1)
                    sb.Append("}");
                else
                    sb.Append("},");
            }
            fstream.Close();

            string jsonStr = sb.ToString();
            List<string> spl = jsonStr.Split(new string[] { "}," }, StringSplitOptions.None).ToList<string>();
            List<BsonDocument> bson = new List<BsonDocument>();
            BsonDocument doc = new BsonDocument();
            foreach (string y in spl)
            {
                string temp = string.Empty;
                if (y.EndsWith("}") == false)
                    temp = y + @"}";
                else
                    temp = y;

                BsonDocument document = BsonDocument.Parse(temp);
                bson.Add(document);
            }
            return bson;
        }
    }
}