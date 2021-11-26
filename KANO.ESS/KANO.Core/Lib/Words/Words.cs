using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Replacing;
using Aspose.Words.Tables;
using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace KANO.Core.Lib.Words
{
    public class Words
    {

        private string savedir;
        private Aspose.Words.Document Doc;

        public Words(string dir, string savedir)
        {
            this.savedir = savedir;
            this.Doc = new Aspose.Words.Document(dir);
        }

        public Words ReplaceString(string key, string value)
        {
            try
            {
                FindReplaceOptions options = new FindReplaceOptions();
                this.Doc.Range.Replace(key, value, options);
            } catch(Exception ex)
            {
                Console.WriteLine("{0} Not Found, {1}", key, value);
            }

            return this;
        }

        public Words SetHeaderImageTitle(string ImagePath, string Title, string Address)
        {
            try
            {
                /*
                NodeCollection shapes = myHeader.GetChildNodes(NodeType.Shape, true);
                HeaderFooterCollection headersFooters = Doc.FirstSection.HeadersFooters;
                HeaderFooter myHeader = headersFooters[HeaderFooterType.HeaderPrimary];

                (shapes[0] as Shape).ImageData.SetImage(ImagePath);
                */

                DocumentBuilder builder = new DocumentBuilder(Doc);
                builder.MoveToHeaderFooter(HeaderFooterType.HeaderPrimary);
                builder.InsertImage(ImagePath, RelativeHorizontalPosition.Page, 60, RelativeVerticalPosition.Page, 10, 150, 50, WrapType.Through);

                Shape textbox = new Shape(Doc, ShapeType.TextBox);
                builder.InsertNode(textbox);

                textbox.WrapType = WrapType.Inline;
                textbox.Width = 180;
                textbox.Height = 50;
                textbox.Left = 400;
                textbox.Top = 10;
                textbox.FillColor = Color.Empty;
                textbox.RelativeHorizontalPosition = RelativeHorizontalPosition.Page;
                textbox.RelativeVerticalPosition = RelativeVerticalPosition.Page;
                textbox.Stroked = false;

                // TEXT BOX SETTING END
                Shape line = new Shape(Doc, ShapeType.Line);
                builder.MoveTo(textbox);

                line.Left = 370;
                line.Top = 10;
                line.Height = 40;
                line.Width = 0;
                line.RelativeHorizontalPosition = RelativeHorizontalPosition.Page;
                line.RelativeVerticalPosition = RelativeVerticalPosition.Page;
                line.StrokeColor = Color.LightGray;

                builder.InsertNode(line);
                // LINE SETTING END

                Paragraph cell2Paragraph = new Paragraph(Doc);
                textbox.AppendChild(cell2Paragraph);

                builder.MoveTo(cell2Paragraph);
                builder.Font.Name = "Calibri";
                builder.Font.Size = 7;
                builder.Font.Bold = true;
                builder.Writeln(Title);

                builder.Font.Size = 6;
                builder.Font.Bold = false;
                builder.Writeln(Address);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Not Found, {1}", ImagePath, ex.StackTrace);
            }

            return this;
        }

        public string Save()
        {
            return this.Save(this.savedir);
        }

        public string Save(string savedir)
        {
            this.Doc.Save(savedir, Aspose.Words.SaveFormat.Pdf);

            return savedir;
        }

    }
}
